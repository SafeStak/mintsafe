using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        NetworkContext networkContext,
        CancellationToken ct = default)
    {
        var swTotal = Stopwatch.StartNew();

        // Derive buyer address after getting source UTxO details 
        var (address, buyerAddressException) = await TryGetBuyerAddressAsync(
            nfts, purchaseAttempt, saleContext, ct).ConfigureAwait(false);
        if (address == null)
        { 
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxInfo,
                purchaseAttempt,
                string.Empty,
                Exception: buyerAddressException);
        }

        var tokenMintValues = nfts.Select(n => new NativeAssetValue(
            saleContext.Collection.PolicyId, Convert.ToHexString(Encoding.UTF8.GetBytes(n.AssetName)), 1)).ToArray();
        var txBuildOutputs = GetTxBuildOutputs(saleContext.Sale, purchaseAttempt, address, tokenMintValues);
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

        var (txRawBytes, txRawException) = await TryGetTxRawBytesAsync(
            txBuildCommand, nfts, saleContext, ct).ConfigureAwait(false);
        if (txRawBytes == null)
        {
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxBuild,
                purchaseAttempt,
                string.Empty,
                Exception: txRawException);
        }

        var (txHash, txSubmissionException) = await TrySubmitTxAsync(
            txRawBytes, nfts, saleContext, ct).ConfigureAwait(false);
        if (txHash == null)
        {
            // TODO: Record a mint in our table storage (see NiftyTypes)
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxSubmit,
                purchaseAttempt,
                txBuildJson,
                Exception: txSubmissionException);
        }

        _instrumentor.TrackDependency(
            EventIds.DistributorElapsed,
            swTotal.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(NiftyDistributor),
            address,
            nameof(DistributeNiftiesForSalePurchase),
            data: txBuildJson,
            isSuccessful: true);

        return new NiftyDistributionResult(
            NiftyDistributionOutcome.Successful, 
            purchaseAttempt,
            txBuildJson,
            txHash,
            address,
            nfts);
    }

    private async Task<(string? Address, Exception? Ex)> TryGetBuyerAddressAsync(
        Nifty[] nfts, PurchaseAttempt purchaseAttempt, SaleContext saleContext, CancellationToken ct)
    {
        try
        {
            var txIo = await _txRetriever.GetTxInfoAsync(purchaseAttempt.Utxo.TxHash, ct).ConfigureAwait(false);
            return (txIo.Inputs.First().Address, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxInfoRetrievalError, ex, $"Failed TxInfo Restrieval");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
    }

    private async Task<(byte[]? TxRawBytes, Exception? Ex)> TryGetTxRawBytesAsync(
        TxBuildCommand txBuildCommand,
        Nifty[] nfts, 
        SaleContext saleContext, 
        CancellationToken ct)
    {
        try
        {
            var raw = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
            return (raw, null);
        }
        catch (CardanoCliException ex)
        {
            _logger.LogError(EventIds.TxBuilderError, ex, $"Failed Tx Build {ex.Args}");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxBuilderError, ex, "Failed Tx Build");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
    }

    private async Task<(string? TxHash, Exception? Ex)> TrySubmitTxAsync(
        byte[] txRawBytes,
        Nifty[] nfts,
        SaleContext saleContext,
        CancellationToken ct)
    {
        try
        {
            var txHash = await _txSubmitter.SubmitTxAsync(txRawBytes, ct).ConfigureAwait(false);
            _logger.LogDebug($"{nameof(_txSubmitter.SubmitTxAsync)} completed with txHash:{txHash}");
            return (txHash, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxSubmissionError, ex, $"Failed Tx Submission");
            await _saleContextStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
    }

    private static TxBuildOutput[] GetTxBuildOutputs(
        Sale sale,
        PurchaseAttempt purchaseAttempt,
        string buyerAddress, 
        NativeAssetValue[] tokenMintValues)
    {
        // Chicken-and-egg bit to calculate the minimum output lovelace value after building the tx output back to the buyer
        // Then mutating the lovelace value quantity with the calculated minLovelaceUtxo
        var buyerOutputUtxoValues = new AggregateValue(0, tokenMintValues);
        var minLovelaceUtxo = TxUtils.CalculateMinUtxoLovelace(buyerOutputUtxoValues);
        ulong buyerLovelacesReturned = minLovelaceUtxo + purchaseAttempt.ChangeInLovelace;
        buyerOutputUtxoValues.Lovelaces = buyerLovelacesReturned;

        var saleLovelaces = purchaseAttempt.Utxo.Lovelaces - buyerLovelacesReturned;
        // No NFT creator address specified or we take 100% of the cut
        if (string.IsNullOrWhiteSpace(sale.CreatorAddress) || sale.PostPurchaseMargin == 1)
        {
            return new[] {
                new TxBuildOutput(buyerAddress, buyerOutputUtxoValues),
                new TxBuildOutput(
                    sale.ProceedsAddress, 
                    new AggregateValue(saleLovelaces, Array.Empty<NativeAssetValue>()), 
                    IsFeeDeducted: true)
            };
        }

        // Calculate proceeds of ADA from saleContext.Sale to creator and proceeds cut
        var proceedsCutLovelaces = (ulong)(saleLovelaces * sale.PostPurchaseMargin);
        var creatorCutLovelaces = saleLovelaces - proceedsCutLovelaces;
        var creatorAddressUtxoValues = new AggregateValue(creatorCutLovelaces, Array.Empty<NativeAssetValue>());
        var proceedsAddressUtxoValues = new AggregateValue(proceedsCutLovelaces, Array.Empty<NativeAssetValue>());
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

public class CardanoSharpNiftyDistributor : INiftyDistributor
{
    private readonly ILogger<CardanoSharpNiftyDistributor> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly ITxInfoRetriever _txRetriever;
    private readonly IMintingKeychainRetriever _keychainRetriever;
    private readonly IMintTransactionBuilder _txBuilder;
    private readonly ITxSubmitter _txSubmitter;
    private readonly ISaleAllocationStore _saleAllocationStore;

    public CardanoSharpNiftyDistributor(
        ILogger<CardanoSharpNiftyDistributor> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        ITxInfoRetriever txRetriever,
        IMintingKeychainRetriever keychainRetriever,
        IMintTransactionBuilder txBuilder,
        ITxSubmitter txSubmitter,
        ISaleAllocationStore saleContextStore)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _txRetriever = txRetriever;
        _keychainRetriever = keychainRetriever;
        _txBuilder = txBuilder;
        _txSubmitter = txSubmitter;
        _saleAllocationStore = saleContextStore;
    }

    public async Task<NiftyDistributionResult> DistributeNiftiesForSalePurchase(
        Nifty[] nfts,
        PurchaseAttempt purchaseAttempt,
        SaleContext saleContext,
        NetworkContext networkContext,
        CancellationToken ct = default)
    {
        var swTotal = Stopwatch.StartNew();

        // Derive buyer address after getting source UTxO details 
        var (address, buyerAddressException) = await TryGetBuyerAddressAsync(
            nfts, purchaseAttempt, saleContext, ct).ConfigureAwait(false);
        if (address == null)
        {
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxInfo,
                purchaseAttempt,
                string.Empty,
                Exception: buyerAddressException);
        }

        // Construct BuildTransactionCommand
        var mintingKeychain = await _keychainRetriever.GetMintingKeyChainAsync(saleContext, ct).ConfigureAwait(false);
        var tokensToMint = nfts.Select(n => new NativeAssetValue(saleContext.Collection.PolicyId, Convert.ToHexString(Encoding.UTF8.GetBytes(n.AssetName)), 1)).ToArray();
        var mint = new[] { new Mint(mintingKeychain.MintingPolicy, tokensToMint) };
        var txCommand = new BuildTransactionCommand(
            Inputs: new[] { purchaseAttempt.Utxo },
            Outputs: GetTxBuildOutputs(saleContext.Sale, purchaseAttempt, address, tokensToMint),
            Mint: mint,
            Metadata: MetadataBuilder.BuildNftMintMetadata(nfts, saleContext.Collection),
            Network: _settings.Network,
            PaymentSigningKeys: mintingKeychain.SigningKeys);

        var (tx, txRawException) = await TryBuildTxRawBytesAsync(
            txCommand, nfts, saleContext, networkContext, ct).ConfigureAwait(false);
        if (tx == null)
        {
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxBuild,
                purchaseAttempt,
                null,
                Exception: txRawException);
        }

        var (txHash, txSubmissionException) = await TrySubmitTxAsync(
            tx.Bytes, nfts, saleContext, ct).ConfigureAwait(false);
        if (txHash == null)
        {
            // TODO: Record a mint in our table storage (see NiftyTypes)
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxSubmit,
                purchaseAttempt,
                Convert.ToHexString(tx.Bytes),
                Exception: txSubmissionException);
        }
        if (txHash != tx.TxHash)
        {
            _logger.LogWarning("Submitted TxHash {txHashSubmitted} is different to calculated TxHash {txHashCalculated}", txHash, tx.TxHash);
        }

        _instrumentor.TrackDependency(
            EventIds.DistributorElapsed,
            swTotal.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(NiftyDistributor),
            address,
            nameof(DistributeNiftiesForSalePurchase),
            isSuccessful: true);

        return new NiftyDistributionResult(
            NiftyDistributionOutcome.Successful,
            purchaseAttempt,
            Convert.ToHexString(tx.Bytes),
            MintTxHash: tx.TxHash,
            BuyerAddress: address,
            NiftiesDistributed: nfts);
    }

    private async Task<(string? Address, Exception? Ex)> TryGetBuyerAddressAsync(
        Nifty[] nfts, PurchaseAttempt purchaseAttempt, SaleContext saleContext, CancellationToken ct)
    {
        try
        {
            var txIo = await _txRetriever.GetTxInfoAsync(purchaseAttempt.Utxo.TxHash, ct).ConfigureAwait(false);
            return (txIo.Inputs.First().Address, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxInfoRetrievalError, ex, $"Failed TxInfo Restrieval");
            await _saleAllocationStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
    }

    private static PendingTransactionOutput[] GetTxBuildOutputs(
        Sale sale,
        PurchaseAttempt purchaseAttempt,
        string buyerAddress,
        NativeAssetValue[] tokenMintValues)
    {
        var minLovelaceUtxo = TxUtils.CalculateMinUtxoLovelace(tokenMintValues);
        ulong buyerLovelacesReturned = minLovelaceUtxo + purchaseAttempt.ChangeInLovelace;
        var buyerOutputUtxoValues = new AggregateValue(buyerLovelacesReturned, tokenMintValues);

        var saleLovelaces = purchaseAttempt.Utxo.Lovelaces - buyerLovelacesReturned;
        // No NFT creator address specified or we take 100% of the cut
        if (string.IsNullOrWhiteSpace(sale.CreatorAddress) || sale.PostPurchaseMargin == 1)
        {
            return new[] {
                new PendingTransactionOutput(buyerAddress, buyerOutputUtxoValues),
                new PendingTransactionOutput(
                    sale.ProceedsAddress,
                    new AggregateValue(saleLovelaces, Array.Empty<NativeAssetValue>()))
            };
        }

        // Calculate proceeds of ADA from saleContext.Sale to creator and proceeds cut
        var proceedsCutLovelaces = (ulong)(saleLovelaces * sale.PostPurchaseMargin);
        var creatorCutLovelaces = saleLovelaces - proceedsCutLovelaces;
        var creatorAddressUtxoValues = new AggregateValue(creatorCutLovelaces, Array.Empty<NativeAssetValue>());
        var proceedsAddressUtxoValues = new AggregateValue(proceedsCutLovelaces, Array.Empty<NativeAssetValue>());
        return new[] {
            new PendingTransactionOutput(buyerAddress, buyerOutputUtxoValues),
            new PendingTransactionOutput(sale.CreatorAddress, creatorAddressUtxoValues),
            new PendingTransactionOutput(sale.ProceedsAddress, proceedsAddressUtxoValues)
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

    private async Task<(BuiltTransaction? BuiltTx, Exception? Ex)> TryBuildTxRawBytesAsync(
        BuildTransactionCommand txBuildCommand,
        Nifty[] nfts,
        SaleContext saleContext,
        NetworkContext networkContext,
        CancellationToken ct)
    {
        try
        {
            var raw = _txBuilder.BuildTx(txBuildCommand, networkContext);
            return (raw, null);
        }
        catch (CardanoSharpException ex)
        {
            _logger.LogError(EventIds.TxBuilderError, ex, "Failed Tx Build");
            await _saleAllocationStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxBuilderError, ex, "Failed Tx Build");
            await _saleAllocationStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
    }

    private async Task<(string? TxHash, Exception? Ex)> TrySubmitTxAsync(
        byte[] txRawBytes,
        Nifty[] nfts,
        SaleContext saleContext,
        CancellationToken ct)
    {
        try
        {
            var txHash = await _txSubmitter.SubmitTxAsync(txRawBytes, ct).ConfigureAwait(false);
            _logger.LogDebug($"{nameof(_txSubmitter.SubmitTxAsync)} completed with txHash:{txHash}");
            return (txHash.TrimStart('"').TrimEnd('"'), null); // Both Blockfrost and Koios add extra " chars at the start and end
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.TxSubmissionError, ex, $"Failed Tx Submission");
            await _saleAllocationStore.ReleaseAllocationAsync(nfts, saleContext, ct).ConfigureAwait(false);
            return (null, ex);
        }
    }
}