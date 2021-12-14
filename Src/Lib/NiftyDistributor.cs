using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class NiftyDistributor : INiftyDistributor
{
    private readonly ILogger<NiftyDistributor> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly IMetadataFileGenerator _metadataGenerator;
    private readonly ITxInfoRetriever _txRetriever;
    private readonly ITxBuilder _txBuilder;
    private readonly ITxSubmitter _txSubmitter;
    private readonly ISaleAllocationStore _saleContextStore;

    public NiftyDistributor(
        ILogger<NiftyDistributor> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        IMetadataFileGenerator metadataGenerator,
        ITxInfoRetriever txRetriever,
        ITxBuilder txBuilder,
        ITxSubmitter txSubmitter,
        ISaleAllocationStore saleContextStore)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _metadataGenerator = metadataGenerator;
        _txRetriever = txRetriever;
        _txBuilder = txBuilder;
        _txSubmitter = txSubmitter;
        _saleContextStore = saleContextStore;
    }

    public async Task<NiftyDistributionResult> DistributeNiftiesForSalePurchase(
        Nifty[] nfts,
        PurchaseAttempt purchaseAttempt,
        SaleContext saleContext,
        CancellationToken ct = default)
    {
        var swTotal = Stopwatch.StartNew();

        // Derive buyer address after getting source UTxO details 
        var buyerAddress = string.Empty;
        try
        {
            var txIo = await _txRetriever.GetTxInfoAsync(purchaseAttempt.Utxo.TxHash, ct).ConfigureAwait(false);
            buyerAddress = txIo.Inputs.First().Address;
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxInfoRetrievalError, ex, $"Failed TxInfo Restrieval");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxInfo,
                purchaseAttempt,
                string.Empty,
                Exception: ex);
        }

        var tokenMintValues = nfts.Select(n => new Value($"{saleContext.Collection.PolicyId}.{n.AssetName}", 1)).ToArray();
        var txBuildOutputs = GetTxBuildOutputs(saleContext.Sale, purchaseAttempt, buyerAddress, tokenMintValues);
        var policyScriptPath = Path.Combine(_settings.BasePath, $"{saleContext.Collection.PolicyId}.policy.script");
        var metadataJsonPath = Path.Combine(_settings.BasePath, $"metadata-mint-{purchaseAttempt.Utxo}.json");
        await _metadataGenerator.GenerateNftStandardMetadataJsonFile(nfts, saleContext.Collection, metadataJsonPath, ct).ConfigureAwait(false);
        var slotExpiry = GetUtxoSlotExpiry(saleContext.Collection, _settings.Network);
        var signingKeyFilePaths = new[]
            {
                Path.Combine(_settings.BasePath, $"{saleContext.Collection.PolicyId}.policy.skey"),
                Path.Combine(_settings.BasePath, $"{saleContext.Sale.Id}.sale.skey")
            };

        var txBuildCommand = new TxBuildCommand(
            new[] { purchaseAttempt.Utxo },
            txBuildOutputs,
            tokenMintValues,
            policyScriptPath,
            metadataJsonPath,
            slotExpiry,
            signingKeyFilePaths);

        // Log tx build command 
        var txBuildJson = JsonSerializer.Serialize(txBuildCommand);
        var utxoFolderPath = Path.Combine(saleContext.SaleUtxosPath, purchaseAttempt.Utxo.ToString());
        if (Directory.Exists(utxoFolderPath))
        {
            var utxoPurchasePath = Path.Combine(utxoFolderPath, "mint_tx.json");
            await File.WriteAllTextAsync(utxoPurchasePath, JsonSerializer.Serialize(txBuildCommand), ct).ConfigureAwait(false);
        }

        var txSubmissionBody = Array.Empty<byte>();
        try
        {
            txSubmissionBody = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
        }
        catch (CardanoCliException ex)
        {
            _logger.LogError(EventIds.TxBuilderError, ex, $"Failed Tx Build {ex.Args}");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxBuild,
                purchaseAttempt,
                txBuildJson + " | " + ex.Args,
                Exception: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxBuilderError, ex, "Failed Tx Build");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxBuild,
                purchaseAttempt,
                txBuildJson,
                Exception: ex);
        }

        var txHash = string.Empty;
        try
        {
            txHash = await _txSubmitter.SubmitTxAsync(txSubmissionBody, ct).ConfigureAwait(false);
            _logger.LogDebug($"{nameof(_txSubmitter.SubmitTxAsync)} completed with txHash:{txHash}");
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxSubmissionError, ex, $"Failed Tx Submission");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxSubmit,
                purchaseAttempt,
                txBuildJson,
                Exception: ex);
        }

        _instrumentor.TrackDependency(
            EventIds.DistributorElapsed,
            swTotal.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(NiftyDistributor),
            buyerAddress,
            nameof(DistributeNiftiesForSalePurchase),
            data: txBuildJson,
            isSuccessful: true);

        return new NiftyDistributionResult(
            NiftyDistributionOutcome.Successful, 
            purchaseAttempt,
            txBuildJson,
            txHash,
            buyerAddress,
            nfts);
    }

    private static Value[] GetBuyerTxOutputUtxoValues(
        Value[] tokenMintValues, long lovelacesReturned)
    {
        var tokenOutputUtxoValues = new Value[tokenMintValues.Length + 1];
        tokenOutputUtxoValues[0] = new Value(Assets.LovelaceUnit, lovelacesReturned);
        for (var i = 0; i < tokenMintValues.Length; i++)
        {
            tokenOutputUtxoValues[i+1] = tokenMintValues[i];
        }
        return tokenOutputUtxoValues;
    }

    private static TxBuildOutput[] GetTxBuildOutputs(
        Sale sale,
        PurchaseAttempt purchaseAttempt,
        string buyerAddress, 
        Value[] tokenMintValues)
    {
        // Chicken-and-egg bit to calculate the minimum output lovelace value after building the tx output back to the buyer
        // Then mutating the lovelace value quantity with the calculated minLovelaceUtxo
        var buyerOutputUtxoValues = GetBuyerTxOutputUtxoValues(tokenMintValues, lovelacesReturned: 0);
        var minLovelaceUtxo = TxUtils.CalculateMinUtxoLovelace(buyerOutputUtxoValues);
        long buyerLovelacesReturned = minLovelaceUtxo + purchaseAttempt.ChangeInLovelace;
        buyerOutputUtxoValues[0].Quantity = buyerLovelacesReturned;

        var saleLovelaces = purchaseAttempt.Utxo.Lovelaces - buyerLovelacesReturned;
        // No NFT creator address specified or we take 100% of the cut
        if (string.IsNullOrWhiteSpace(sale.CreatorAddress) || sale.PostPurchaseMargin == 1)
        {
            return new[] {
                new TxBuildOutput(buyerAddress, buyerOutputUtxoValues),
                new TxBuildOutput(
                    sale.ProceedsAddress, 
                    new[] { new Value(Assets.LovelaceUnit, saleLovelaces) }, 
                    IsFeeDeducted: true)
            };
        }

        // Calculate proceeds of ADA from saleContext.Sale to creator and proceeds cut
        var proceedsCutLovelaces = (int)(saleLovelaces * sale.PostPurchaseMargin);
        var creatorCutLovelaces = saleLovelaces - proceedsCutLovelaces;
        var creatorAddressUtxoValues = new[] { new Value(Assets.LovelaceUnit, creatorCutLovelaces) };
        var proceedsAddressUtxoValues = new[] { new Value(Assets.LovelaceUnit, proceedsCutLovelaces) };
        return new[] {
            new TxBuildOutput(buyerAddress, buyerOutputUtxoValues),
            new TxBuildOutput(sale.CreatorAddress, creatorAddressUtxoValues, IsFeeDeducted: true),
            new TxBuildOutput(sale.ProceedsAddress, proceedsAddressUtxoValues)
        };
    }

    private static long GetUtxoSlotExpiry(
        NiftyCollection collection, Network network)
    {
        // Can override the slot expiry at the collection level
        if (collection.SlotExpiry >= 0)
        {
            return collection.SlotExpiry;
        }
        // Derive from LockedAt date
        return network == Network.Mainnet
            ? TimeUtil.GetMainnetSlotAt(collection.LockedAt)
            : TimeUtil.GetTestnetSlotAt(collection.LockedAt);
    }
}
