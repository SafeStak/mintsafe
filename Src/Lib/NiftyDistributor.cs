﻿using Microsoft.Extensions.Logging;
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
            tx.CborBytes, nfts, saleContext, ct).ConfigureAwait(false);
        if (txHash == null)
        {
            // TODO: Record a mint in our table storage (see NiftyTypes)
            return new NiftyDistributionResult(
                NiftyDistributionOutcome.FailureTxSubmit,
                purchaseAttempt,
                Convert.ToHexString(tx.CborBytes),
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
            nameof(CardanoSharpNiftyDistributor),
            address,
            nameof(DistributeNiftiesForSalePurchase),
            isSuccessful: true);

        return new NiftyDistributionResult(
            NiftyDistributionOutcome.Successful,
            purchaseAttempt,
            Convert.ToHexString(tx.CborBytes),
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
        var buyerOutputUtxoValues = new Balance(buyerLovelacesReturned, tokenMintValues);

        var saleLovelaces = purchaseAttempt.Utxo.Lovelaces - buyerLovelacesReturned;
        // No NFT creator address specified or we take 100% of the cut
        if (string.IsNullOrWhiteSpace(sale.CreatorAddress) || sale.PostPurchaseMargin == 1)
        {
            return new[] {
                new PendingTransactionOutput(buyerAddress, buyerOutputUtxoValues),
                new PendingTransactionOutput(
                    sale.ProceedsAddress,
                    new Balance(saleLovelaces, Array.Empty<NativeAssetValue>()))
            };
        }

        // Calculate proceeds of ADA from saleContext.Sale to creator and proceeds cut
        var proceedsCutLovelaces = (ulong)(saleLovelaces * sale.PostPurchaseMargin);
        var creatorCutLovelaces = saleLovelaces - proceedsCutLovelaces;
        var creatorAddressUtxoValues = new Balance(creatorCutLovelaces, Array.Empty<NativeAssetValue>());
        var proceedsAddressUtxoValues = new Balance(proceedsCutLovelaces, Array.Empty<NativeAssetValue>());
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