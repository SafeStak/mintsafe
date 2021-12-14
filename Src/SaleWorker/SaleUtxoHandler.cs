using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        var handlingOutcome = "FailedUnknown";
        var shouldRefundUtxo = false;
        var sw = Stopwatch.StartNew();
        try
        {
            var purchase = PurchaseAttemptGenerator.FromUtxo(saleUtxo, saleContext.Sale);
            _logger.LogDebug($"Successfully built purchase request: {purchase.NiftyQuantityRequested} NFTs for {saleUtxo.Lovelaces} and {purchase.ChangeInLovelace} change");
            
            // Log purchase info to file system
            var utxoFolderPath = Path.Combine(saleContext.SaleUtxosPath, saleUtxo.ToString());
            Directory.CreateDirectory(utxoFolderPath);
            var utxoPurchasePath = Path.Combine(utxoFolderPath, "purchase.json");
            await File.WriteAllTextAsync(utxoPurchasePath, JsonSerializer.Serialize(purchase), ct).ConfigureAwait(false);

            // Allocate tokens 
            var tokens = await _tokenAllocator.AllocateNiftiesForPurchaseAsync(purchase, saleContext, ct).ConfigureAwait(false);
            _logger.LogDebug($"Successfully allocated {tokens.Length} tokens");
            var utxoAllocatedPath = Path.Combine(utxoFolderPath, "allocation.csv");
            await File.WriteAllLinesAsync(utxoAllocatedPath, tokens.Select(n => n.Id.ToString()), ct).ConfigureAwait(false);

            // Distribute tokens 
            var distributionResult = await _tokenDistributor.DistributeNiftiesForSalePurchase(
                tokens, purchase, saleContext, ct).ConfigureAwait(false);
            handlingOutcome = distributionResult.Outcome.ToString();
            var utxoDistributionPath = Path.Combine(utxoFolderPath, "distribution.json");
            await File.WriteAllTextAsync(utxoDistributionPath, JsonSerializer.Serialize(
                new { Outcome = distributionResult.Outcome.ToString(), distributionResult.MintTxHash, 
                    distributionResult.BuyerAddress, distributionResult.NiftiesDistributed }), ct).ConfigureAwait(false);
            if (distributionResult.Outcome == NiftyDistributionOutcome.Successful
                || distributionResult.Outcome == NiftyDistributionOutcome.SuccessfulAfterRetry)
            {
                _logger.LogDebug($"Successfully distributed {tokens.Length} tokens from Tx {distributionResult.MintTxHash}");
                saleContext.SuccessfulUtxos.Add(saleUtxo);
                isSuccessful = true;
            }
        }
        catch (SaleInactiveException ex)
        {
            _logger.LogError(EventIds.SaleInactive, ex, "Sale is Inactive (flagged by publisher)");
            shouldRefundUtxo = true;
            handlingOutcome = "SaleNotActive";
        }
        catch (SalePeriodOutOfRangeException ex)
        {
            _logger.LogError(EventIds.SalePeriodOutOfRange, ex, "Sale is Inactive (outside start/end period)");
            shouldRefundUtxo = true;
            handlingOutcome = "OutsideSalePeriod";
        }
        catch (InsufficientPaymentException ex)
        {
            _logger.LogError(EventIds.InsufficientPayment, ex, $"Insufficient payment received for sale {ex.PurchaseAttemptUtxo.Lovelaces}");
            shouldRefundUtxo = true;
            handlingOutcome = "InsufficientSalePurchase";
        }
        catch (MaxAllowedPurchaseQuantityExceededException ex)
        {
            _logger.LogError(EventIds.MaxAllowedPurchaseQuantityExceeded, ex, $"Payment attempted to purchase too many for sale {ex.DerivedQuantity} vs {ex.MaxQuantity}");
            shouldRefundUtxo = true;
            handlingOutcome = "SaleMaxPurchaseQuantityExceeded";
        }
        catch (PurchaseQuantityHardLimitException ex)
        {
            _logger.LogError(EventIds.PurchaseQuantityHardLimitExceeded, ex, $"Purchase quantity {ex.RequestedQuantity} greater than hard limit");
            shouldRefundUtxo = true;
            handlingOutcome = "MintsafeMaxPurchaseQuantityExceeded";
        }
        catch (CannotAllocateMoreThanSaleReleaseException ex)
        {
            _logger.LogError(EventIds.CannotAllocateMoreThanSaleRelease, ex, $"Sale allocation exceeded release {ex.RequestedQuantity} vs {ex.SaleAllocatedQuantity}/{ex.SaleReleaseQuantity}");
            shouldRefundUtxo = true;
            handlingOutcome = "SoldOut";
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.SaleHandlerUnhandledError, ex, "Unhandled exception");
            handlingOutcome = "UnhandledException";
            // Don't refund in case we can handle the utxo again
        }
        finally
        {
            saleContext.LockedUtxos.Add(saleUtxo);
            if (!isSuccessful)
            {
                saleContext.FailedUtxos.Add(saleUtxo);
            }
            if (shouldRefundUtxo)
            {
                await RefundUtxo(saleUtxo, saleContext, handlingOutcome, ct).ConfigureAwait(false);
            }
            LogHandlingRequest(saleUtxo.ToString(), isSuccessful, sw.ElapsedMilliseconds, handlingOutcome, saleContext);
        }
    }

    private void LogHandlingRequest(
        string saleUtxo,
        bool isSuccessful,
        long elapsedMilliseconds,
        string handlingOutcome,
        SaleContext saleContext)
    {
        var additionalProperties = new Dictionary<string, object>
            {
                { "WorkerId", saleContext.SaleWorkerId },
                { "SaleId", saleContext.Sale.Id },
                { "CollectionId", saleContext.Collection.Id },
                { "Utxo", saleUtxo.ToString() },
                { "Outcome", handlingOutcome },
                { "SaleContext.AllocatedTokens", saleContext.AllocatedTokens.Count },
                { "SaleContext.MintableTokens", saleContext.MintableTokens.Count },
                { "SaleContext.RefundedUtxos", saleContext.RefundedUtxos.Count },
                { "SaleContext.SuccessfulUtxos", saleContext.SuccessfulUtxos.Count },
                { "SaleContext.FailedUtxos", saleContext.FailedUtxos.Count },
                { "SaleContext.LockedUtxos", saleContext.LockedUtxos.Count },
            };
        _instrumentor.TrackRequest(
            EventIds.SaleHandlerElapsed,
            elapsedMilliseconds,
            DateTime.UtcNow,
            nameof(SaleUtxoHandler),
            source: saleUtxo.ToString(),
            isSuccessful: isSuccessful,
            customProperties: additionalProperties);
    }

    private async Task RefundUtxo(
        Utxo saleUtxo, SaleContext saleContext, string refundReason, CancellationToken ct)
    {
        try
        {
            // TODO: better way to do refunds? Use Channels?
            var saleAddressSigningKey = Path.Combine(_settings.BasePath, $"{saleContext.Sale.Id}.sale.skey");
            var refundTxHash = await _utxoRefunder.ProcessRefundForUtxo(saleUtxo, saleAddressSigningKey, refundReason, ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(refundTxHash))
            {
                saleContext.RefundedUtxos.Add(saleUtxo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.UtxoRefunderError, ex, $"Refund error for {saleUtxo} {refundReason}");
        }
    }
}
