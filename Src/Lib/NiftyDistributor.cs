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
    private readonly ISaleContextDataStorage _saleContextStore;

    public NiftyDistributor(
        ILogger<NiftyDistributor> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        IMetadataFileGenerator metadataGenerator,
        ITxInfoRetriever txRetriever,
        ITxBuilder txBuilder,
        ITxSubmitter txSubmitter,
        ISaleContextDataStorage saleContextStore)
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
        var sw = Stopwatch.StartNew();
        // Generate metadata file
        var metadataJsonFileName = $"metadata-mint-{purchaseAttempt.Utxo}.json";
        var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
        await _metadataGenerator.GenerateNftStandardMetadataJsonFile(nfts, saleContext.Collection, metadataJsonPath, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_metadataGenerator.GenerateNftStandardMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

        // Derive buyer address after getting source UTxO details 
        sw.Restart();
        var buyerAddress = string.Empty;
        try
        {
            var txIo = await _txRetriever.GetTxInfoAsync(purchaseAttempt.Utxo.TxHash, ct).ConfigureAwait(false);
            buyerAddress = txIo.Inputs.First().Address;
        }
        catch (Exception ex)
        {
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxInfo,
                purchaseAttempt,
                string.Empty,
                Exception: ex);
        }

        /// Map UtxoValues for new tokens
        var tokenMintUtxoValues = nfts.Select(n => new Value($"{saleContext.Collection.PolicyId}.{n.AssetName}", 1)).ToArray();
        // Chicken-and-egg bit to calculate the minimum output lovelace value after building the tx output back to the buyer
        // Then mutating the lovelace value quantity with the calculated minLovelaceUtxo
        var buyerOutputUtxoValues = GetBuyerTxOutputUtxoValues(tokenMintUtxoValues, lovelacesReturned: 0);
        var minLovelaceUtxo = TxUtils.CalculateMinUtxoLovelace(buyerOutputUtxoValues);
        long buyerLovelacesReturned = minLovelaceUtxo + purchaseAttempt.ChangeInLovelace;
        buyerOutputUtxoValues[0].Quantity = buyerLovelacesReturned;
        // Calculate proceeds of ADA from saleContext.Sale to creator and mintsafe's cut
        var saleLovelaces = purchaseAttempt.Utxo.Lovelaces - buyerLovelacesReturned;
        var mintsafeCutLovelaces = (int)(saleLovelaces * saleContext.Sale.PostPurchaseMargin);
        var creatorCutLovelaces = saleLovelaces - mintsafeCutLovelaces;
        var creatorAddressUtxoValues = new[] { new Value(Assets.LovelaceUnit, creatorCutLovelaces) };
        var proceedsAddressUtxoValues = new[] { new Value(Assets.LovelaceUnit, mintsafeCutLovelaces) };

        var policyScriptFilename = $"{saleContext.Collection.PolicyId}.policy.script";
        var policyScriptPath = Path.Combine(_settings.BasePath, policyScriptFilename);
        var slotExpiry = GetUtxoSlotExpiry(saleContext.Collection, _settings.Network);
        var signingKeyFilePaths = new[]
            {
                Path.Combine(_settings.BasePath, $"{saleContext.Collection.PolicyId}.policy.skey"),
                Path.Combine(_settings.BasePath, $"{saleContext.Sale.Id}.saleContext.Sale.skey")
            };

        var txBuildCommand = new TxBuildCommand(
            new[] { purchaseAttempt.Utxo },
            new[] {
                new TxBuildOutput(buyerAddress, buyerOutputUtxoValues),
                new TxBuildOutput(saleContext.Sale.CreatorAddress, creatorAddressUtxoValues, IsFeeDeducted: true),
                new TxBuildOutput(saleContext.Sale.ProceedsAddress, proceedsAddressUtxoValues)
            },
            tokenMintUtxoValues,
            policyScriptPath,
            metadataJsonPath,
            slotExpiry,
            signingKeyFilePaths);
        var utxoFolderPath = Path.Combine(saleContext.SaleUtxosPath, purchaseAttempt.Utxo.ToString());
        if (File.Exists(utxoFolderPath))
        {
            var utxoPurchasePath = Path.Combine(utxoFolderPath, "mint_tx.json");
            File.WriteAllText(utxoPurchasePath, JsonSerializer.Serialize(txBuildCommand));
        }

        var txSubmissionBody = Array.Empty<byte>();
        sw.Restart();
        try
        {
            txSubmissionBody = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
            _logger.LogDebug($"{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
        }
        catch (CardanoCliException ex)
        {
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxBuild,
                purchaseAttempt,
                JsonSerializer.Serialize(txBuildCommand) + " | " + ex.Args,
                Exception: ex);
        }
        catch (Exception ex)
        {
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxBuild,
                purchaseAttempt,
                JsonSerializer.Serialize(txBuildCommand),
                Exception: ex);
        }

        var txHash = string.Empty;
        sw.Restart();
        try
        {
            txHash = await _txSubmitter.SubmitTxAsync(txSubmissionBody, ct).ConfigureAwait(false);
            _logger.LogDebug($"{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxSubmit,
                purchaseAttempt,
                JsonSerializer.Serialize(txBuildCommand),
                Exception: ex);
        }

        _instrumentor.TrackDependency(
            EventIds.DistributorElapsed,
            swTotal.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(NiftyDistributor),
            string.Empty,
            nameof(DistributeNiftiesForSalePurchase),
            data: JsonSerializer.Serialize(txBuildCommand),
            isSuccessful: true);

        return new NiftyDistributionResult(
            NiftyDistributionOutcome.Successful, 
            purchaseAttempt,
            JsonSerializer.Serialize(txBuildCommand),
            txHash,
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

    private static long GetUtxoSlotExpiry(
        NiftyCollection collection, Network network)
    {
        if (collection.SlotExpiry >= 0)
        {
            return collection.SlotExpiry;
        }

        return network == Network.Mainnet
            ? TimeUtil.GetMainnetSlotAt(collection.LockedAt)
            : TimeUtil.GetTestnetSlotAt(collection.LockedAt);
    }
}
