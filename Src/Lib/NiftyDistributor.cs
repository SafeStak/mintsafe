using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class NiftyDistributor : INiftyDistributor
{
    private const int MinLovelaceUtxo = 2000000;

    private readonly ILogger<NiftyDistributor> _logger;
    private readonly MintsafeSaleWorkerSettings _settings;
    private readonly IMetadataGenerator _metadataGenerator;
    private readonly ITxIoRetriever _txRetriever;
    private readonly ITxBuilder _txBuilder;
    private readonly ITxSubmitter _txSubmitter;

    public NiftyDistributor(
        ILogger<NiftyDistributor> logger,
        MintsafeSaleWorkerSettings settings,
        IMetadataGenerator metadataGenerator,
        ITxIoRetriever txRetriever,
        ITxBuilder txBuilder,
        ITxSubmitter txSubmitter)
    {
        _logger = logger;
        _settings = settings;
        _metadataGenerator = metadataGenerator;
        _txRetriever = txRetriever;
        _txBuilder = txBuilder;
        _txSubmitter = txSubmitter;
    }

    public async Task<string> DistributeNiftiesForSalePurchase(
        Nifty[] nfts,
        PurchaseAttempt purchaseRequest,
        NiftyCollection collection,
        Sale sale,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        // Generate metadata file
        var metadataJsonFileName = $"metadata-mint-{purchaseRequest.Utxo}.json";
        var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
        await _metadataGenerator.GenerateNftStandardMetadataJsonFile(nfts, collection, metadataJsonPath, ct);
        _logger.LogInformation($"{nameof(_metadataGenerator.GenerateNftStandardMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

        // Derive buyer address after getting source UTxO details from Blockfrost
        sw.Restart();
        var txIo = await _txRetriever.GetTxIoAsync(purchaseRequest.Utxo.TxHash, ct);
        var buyerAddress = txIo.Inputs.First().Address;
        _logger.LogInformation($"{nameof(_txRetriever.GetTxIoAsync)} completed after {sw.ElapsedMilliseconds}ms");

        // Map UtxoValues for new tokens
        long buyerLovelacesReturned = MinLovelaceUtxo + purchaseRequest.ChangeInLovelace;
        var tokenMintUtxoValues = nfts.Select(n => new Value($"{collection.PolicyId}.{n.AssetName}", 1)).ToArray();
        var buyerOutputUtxoValues = GetBuyerTxOutputUtxoValues(tokenMintUtxoValues, buyerLovelacesReturned);
        var proceedsAddressLovelaces = purchaseRequest.Utxo.Lovelaces - buyerLovelacesReturned;
        var proceedsAddressUtxoValues = new[] { new Value(Assets.LovelaceUnit, proceedsAddressLovelaces) };

        var policyScriptFilename = $"{collection.PolicyId}.policy.script";
        var policyScriptPath = Path.Combine(_settings.BasePath, policyScriptFilename);
        var slotExpiry = GetUtxoSlotExpiry(collection, _settings.Network);
        var signingKeyFilePaths = new[]
        {
                Path.Combine(_settings.BasePath, $"{collection.PolicyId}.policy.skey"),
                Path.Combine(_settings.BasePath, $"{sale.Id}.sale.skey")
            };

        var txBuildCommand = new TxBuildCommand(
            new[] { purchaseRequest.Utxo },
            new[] {
                    new TxBuildOutput(buyerAddress, buyerOutputUtxoValues),
                    new TxBuildOutput(sale.ProceedsAddress, proceedsAddressUtxoValues, IsFeeDeducted: true) },
            tokenMintUtxoValues,
            policyScriptPath,
            metadataJsonPath,
            slotExpiry,
            signingKeyFilePaths);

        sw.Restart();
        var txSubmissionBody = await _txBuilder.BuildTxAsync(txBuildCommand, ct);
        _logger.LogInformation($"{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var txHash = await _txSubmitter.SubmitTxAsync(txSubmissionBody, ct);
        _logger.LogInformation($"{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

        return txHash;
    }

    private static Value[] GetBuyerTxOutputUtxoValues(
        Value[] tokenMintValues, long lovelacesReturned)
    {
        var tokenOutputUtxoValues = new Value[tokenMintValues.Length + 1];
        for (var i = 0; i < tokenMintValues.Length; i++)
        {
            tokenOutputUtxoValues[i] = tokenMintValues[i];
        }
        tokenOutputUtxoValues[tokenMintValues.Length] = new Value(Assets.LovelaceUnit, lovelacesReturned);
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
