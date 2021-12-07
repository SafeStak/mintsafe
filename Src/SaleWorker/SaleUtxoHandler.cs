using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.SaleWorker;

public interface ISaleUtxoHandler
{
    Task HandleAsync(Utxo saleUtxo, SaleContext saleContext, CancellationToken ct);
}

public class SaleUtxoHandler : ISaleUtxoHandler
{
    private readonly ILogger<SaleUtxoHandler> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly INiftyAllocator _tokenAllocator;
    private readonly INiftyDistributor _tokenDistributor;
    private readonly IUtxoRefunder _utxoRefunder;

    public SaleUtxoHandler(
        ILogger<SaleUtxoHandler> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        INiftyAllocator tokenAllocator,
        INiftyDistributor tokenDistributor,
        IUtxoRefunder utxoRefunder)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _tokenAllocator = tokenAllocator;
        _tokenDistributor = tokenDistributor;
        _utxoRefunder = utxoRefunder;
    }

    public async Task HandleAsync(
        Utxo saleUtxo,
        SaleContext saleContext,
        CancellationToken ct)
    {
        var isSuccessful = false;
        var shouldRefundUtxo = false;
        var refundReason = string.Empty;
        var sw = Stopwatch.StartNew();
        try
        {
            var purchaseRequest = PurchaseAttemptGenerator.FromUtxo(saleUtxo, saleContext.Sale);
            _logger.LogDebug($"Successfully built purchase request: {purchaseRequest.NiftyQuantityRequested} NFTs for {saleUtxo.Lovelaces} and {purchaseRequest.ChangeInLovelace} change");

            var tokens = await _tokenAllocator.AllocateNiftiesForPurchaseAsync(
                purchaseRequest, saleContext.AllocatedTokens, saleContext.MintableTokens, saleContext.Sale, ct);
            _logger.LogDebug($"Successfully allocated {tokens.Length} tokens");

            var distributionResult = await _tokenDistributor.DistributeNiftiesForSalePurchase(
                tokens, purchaseRequest, saleContext.Collection, saleContext.Sale, ct);
            if (distributionResult.Outcome == NiftyDistributionOutcome.Successful
                || distributionResult.Outcome == NiftyDistributionOutcome.SuccessfulAfterRetry)
            {
                _logger.LogDebug($"Successfully distributed {tokens.Length} tokens from Tx {distributionResult.MintTxHash}");
                saleContext.SuccessfulUtxos.Add(saleUtxo);
                isSuccessful = true;
            }
            else
            {
                _logger.LogWarning($"Failed distribution of {tokens.Length} tokens for {distributionResult.PurchaseAttempt.Utxo}\n{distributionResult.Exception}\n{distributionResult.MintTxBodyJson}");
            }
        }
        catch (SaleInactiveException ex)
        {
            _logger.LogError(LogEventIds.SaleInactive, ex, "Sale is Inactive (flagged by publisher)");
            shouldRefundUtxo = true;
            refundReason = "saleinactive";
        }
        catch (SalePeriodOutOfRangeException ex)
        {
            _logger.LogError(LogEventIds.SalePeriodOutOfRange, ex, "Sale is Inactive (outside start/end period)");
            shouldRefundUtxo = true;
            refundReason = "saleperiodout";
        }
        catch (InsufficientPaymentException ex)
        {
            _logger.LogError(LogEventIds.InsufficientPayment, ex, "Insufficient payment received for sale");
            shouldRefundUtxo = true;
            refundReason = "salepaymentinsufficient";
        }
        catch (MaxAllowedPurchaseQuantityExceededException ex)
        {
            _logger.LogError(LogEventIds.MaxAllowedPurchaseQuantityExceeded, ex, "Payment attempted to purchase too many for sale");
            shouldRefundUtxo = true;
            refundReason = "salemaxallowedexceeded";
        }
        catch (CannotAllocateMoreThanSaleReleaseException ex)
        {
            _logger.LogError(LogEventIds.CannotAllocateMoreThanSaleRelease, ex, "Sale allocation exceeded release");
            shouldRefundUtxo = true;
            refundReason = "salefullyallocated";
        }
        catch (CannotAllocateMoreThanMintableException ex)
        {
            _logger.LogError(LogEventIds.CannotAllocateMoreThanSaleRelease, ex, "Sale allocation exceeded mintable");
            shouldRefundUtxo = true;
            refundReason = "collectionfullyminted";
        }
        catch (BlockfrostResponseException ex)
        {
            _logger.LogError(LogEventIds.BlockfrostServerErrorResponse, ex, "Blockfrost API response error");
        }
        catch (CardanoCliException ex)
        {
            _logger.LogError(LogEventIds.CardanoCliUnhandledError, ex, "cardano-cli Error");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEventIds.UnhandledError, ex, "Unhandled exception");
        }
        finally
        {
            saleContext.LockedUtxos.Add(saleUtxo);
            if (shouldRefundUtxo)
            {
                await RefundUtxo(saleUtxo, saleContext, refundReason, ct);
            }
            _instrumentor.TrackRequest(
                EventIds.SaleHandlerElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(SaleUtxoHandler),
                source: saleUtxo.ToString(),
                isSuccessful: isSuccessful,
                customProperties: new Dictionary<string, object>
                {
                    { "WorkerId", saleContext.SaleWorkerId },
                    { "SaleId", saleContext.Sale.Id },
                    { "CollectionId", saleContext.Collection.Id },
                    { "Utxo", saleUtxo.ToString() },
                    { "SaleContext.AllocatedTokens", saleContext.AllocatedTokens.Count },
                    { "SaleContext.MintableTokens", saleContext.MintableTokens.Count },
                    { "SaleContext.RefundedUtxos", saleContext.RefundedUtxos.Count },
                    { "SaleContext.SuccessfulUtxos", saleContext.SuccessfulUtxos.Count },
                    { "SaleContext.LockedUtxos", saleContext.LockedUtxos.Count },
                });
        }
    }

    private async Task RefundUtxo(
        Utxo saleUtxo, SaleContext saleContext, string refundReason, CancellationToken ct)
    {
        try
        {
            // TODO: better way to do refunds? Use Channels?
            var saleAddressSigningKey = Path.Combine(_settings.BasePath, $"{saleContext.Sale.Id}.sale.skey");
            var refundTxHash = await _utxoRefunder.ProcessRefundForUtxo(saleUtxo, saleAddressSigningKey, refundReason, ct);
            if (!string.IsNullOrEmpty(refundTxHash))
            {
                saleContext.RefundedUtxos.Add(saleUtxo);
            }
        }
        catch (Exception ex)
        {
            throw new FailedUtxoRefundException("Unable to process refund", saleContext.Sale.Id, saleUtxo, ex);
        }
    }
}
