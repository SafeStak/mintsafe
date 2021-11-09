using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class TokenDistributor : ITokenDistributor
    {
        private const int MinLovelaceUtxo = 2000000;

        private readonly ILogger<TokenDistributor> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly IMetadataGenerator _metadataGenerator;
        private readonly ITxIoRetriever _txRetriever;
        private readonly ITxBuilder _txBuilder;
        private readonly ITxSubmitter _txSubmitter;

        public TokenDistributor(
            ILogger<TokenDistributor> logger,
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
            // Generate metadata file
            var metadataJsonFileName = $"metadata-mint-{purchaseRequest.Utxo}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateNftStandardMetadataJsonFile(nfts, collection, metadataJsonPath, ct);

            // Derive buyer address after getting source UTxO details from BF
            var txIo = await _txRetriever.GetTxIoAsync(purchaseRequest.Utxo.TxHash, ct);
            var buyerAddress = txIo.Inputs.First().Address;

            // Map UtxoValues for new tokens
            long buyerLovelacesReturned = MinLovelaceUtxo + purchaseRequest.ChangeInLovelace;
            var tokenMintUtxoValues = nfts.Select(n => new UtxoValue($"{collection.PolicyId}.{n.AssetName}", 1)).ToArray();
            var buyerOutputUtxoValues = GetBuyerTxOutputUtxoValues(tokenMintUtxoValues, buyerLovelacesReturned);
            var profitAddressLovelaces = purchaseRequest.Utxo.Lovelaces() - buyerLovelacesReturned;
            var profitAddressUtxoValues = new[] { new UtxoValue("lovelace", profitAddressLovelaces) };

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
                    new TxOutput(buyerAddress, buyerOutputUtxoValues),
                    new TxOutput(sale.ProceedsAddress, profitAddressUtxoValues, IsFeeDeducted: true) },
                tokenMintUtxoValues,
                policyScriptPath,
                metadataJsonPath,
                slotExpiry,
                signingKeyFilePaths);

            var txSubmissionBody = await _txBuilder.BuildTxAsync(txBuildCommand, ct);

            var txHash = await _txSubmitter.SubmitTxAsync(txSubmissionBody, ct);

            return txHash;
        }

        private static UtxoValue[] GetBuyerTxOutputUtxoValues(
            UtxoValue[] tokenMintValues, long lovelacesReturned)
        {
            var tokenOutputUtxoValues = new UtxoValue[tokenMintValues.Length + 1];
            for (var i = 0; i < tokenMintValues.Length; i++)
            {
                tokenOutputUtxoValues[i] = tokenMintValues[i];
            }
            tokenOutputUtxoValues[tokenMintValues.Length] = new UtxoValue("lovelace", lovelacesReturned);
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
}
