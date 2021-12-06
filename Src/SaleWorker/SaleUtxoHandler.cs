using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.SaleWorker;

public interface ISaleUtxoHandler
{
    Task HandleAsync(
        Utxo saleUtxo,
        NiftyCollection collection,
        Sale activeSale,
        SaleContext saleContext,
        CancellationToken ct);
}

public class SaleUtxoHandler : ISaleUtxoHandler
{
    private readonly ILogger<SaleUtxoHandler> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly TelemetryClient _telemetryClient;
    private readonly INiftyAllocator _tokenAllocator;
    private readonly INiftyDistributor _tokenDistributor;
    private readonly IUtxoRefunder _utxoRefunder;

    public SaleUtxoHandler(
        ILogger<SaleUtxoHandler> logger,
        MintsafeAppSettings settings,
        TelemetryClient telemetryClient,
        INiftyAllocator tokenAllocator,
        INiftyDistributor tokenDistributor,
        IUtxoRefunder utxoRefunder)
    {
        _logger = logger;
        _settings = settings;
        _telemetryClient = telemetryClient;
        _tokenAllocator = tokenAllocator;
        _tokenDistributor = tokenDistributor;
        _utxoRefunder = utxoRefunder;
    }

    public async Task HandleAsync(
        Utxo saleUtxo,
        NiftyCollection collection,
        Sale activeSale,
        SaleContext saleContext,
        CancellationToken ct)
    {
        var shouldRefundUtxo = false;
        var refundReason = string.Empty;
        var sw = Stopwatch.StartNew();
        try
        {
            var purchaseRequest = PurchaseAttemptGenerator.FromUtxo(saleUtxo, activeSale);
            _logger.LogDebug($"Successfully built purchase request: {purchaseRequest.NiftyQuantityRequested} NFTs for {saleUtxo.Lovelaces} and {purchaseRequest.ChangeInLovelace} change");

            var tokens = await _tokenAllocator.AllocateNiftiesForPurchaseAsync(
                purchaseRequest, saleContext.AllocatedTokens, saleContext.MintableTokens, activeSale, ct);
            _logger.LogDebug($"Successfully allocated {tokens.Length} tokens");

            var distributionResult = await _tokenDistributor.DistributeNiftiesForSalePurchase(tokens, purchaseRequest, collection, activeSale, ct);
            if (distributionResult.Outcome == NiftyDistributionOutcome.Successful
                || distributionResult.Outcome == NiftyDistributionOutcome.SuccessfulAfterRetry)
            {
                _logger.LogDebug($"Successfully distributed {tokens.Length} tokens from Tx {distributionResult.MintTxHash}");
                saleContext.SuccessfulUtxos.Add(saleUtxo);
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
                // TODO: better way to do refunds? Use Channels?
                var saleAddressSigningKey = Path.Combine(_settings.BasePath, $"{activeSale.Id}.sale.skey");
                var refundTxHash = await _utxoRefunder.ProcessRefundForUtxo(saleUtxo, saleAddressSigningKey, refundReason, ct);
                if (!string.IsNullOrEmpty(refundTxHash))
                {
                    saleContext.RefundedUtxos.Add(saleUtxo);
                }
            }
        }
    }
}
