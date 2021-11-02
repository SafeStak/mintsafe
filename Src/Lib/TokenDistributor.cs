using NiftyLaunchpad.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TokenDistributor
    {
        private const int MinLovelaceUtxo = 2000000;

        private readonly NiftyLaunchpadSettings _settings;
        private readonly IMetadataGenerator _metadataGenerator;
        private readonly ITxRetriever _txRetriever;
        private readonly ITxBuilder _txBuilder;
        private readonly ITxSubmitter _txSubmitter;

        public TokenDistributor(
            NiftyLaunchpadSettings settings,
            IMetadataGenerator metadataGenerator,
            ITxRetriever txRetriever,
            ITxBuilder txBuilder,
            ITxSubmitter txSubmitter)
        {
            _settings = settings;
            _metadataGenerator = metadataGenerator;
            _txRetriever = txRetriever;
            _txBuilder = txBuilder;
            _txSubmitter = txSubmitter;
        }

        public async Task<string> DistributeNiftiesForSalePurchase(
            Nifty[] nfts, 
            NiftySalePurchaseRequest purchaseRequest, 
            NiftyCollection collection,
            NiftySale sale,
            CancellationToken ct = default)
        {
            // Generate metadata file
            var metadataJsonFileName = $"metadata-{purchaseRequest.Utxo}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateMetadataJsonFile(nfts, collection, metadataJsonPath, ct);

            // Derive buyer address after getting source UTxO details from BF
            var txIo = await _txRetriever.GetBasicTxAsync(purchaseRequest.Utxo.TxHash);
            var buyerAddress = txIo.Inputs.First().Address;

            // Map UtxoValues for new tokens
            long buyerLovelacesReturned = MinLovelaceUtxo + purchaseRequest.ChangeInLovelace;
            var tokenMintUtxoValues = nfts.Select(n => new UtxoValue($"{collection.PolicyId}.{n.AssetName}", 1)).ToArray();
            var buyerOutputUtxoValues = GetBuyerTxOutputUtxoValues(tokenMintUtxoValues, buyerLovelacesReturned);
            var profitAddressLovelaces = purchaseRequest.Utxo.Lovelaces() - buyerLovelacesReturned;
            var profitAddressUtxoValues = new[] { new UtxoValue("lovelace", profitAddressLovelaces) };

            //var ttl = _settings.Network == Network.Mainnet
            //    ? TimeUtil.GetMainnetSlotAt(collection.LockedAt)
            //    : TimeUtil.GetTestnetSlotAt(collection.LockedAt);

            var policyScriptFilename = $"{collection.PolicyId}.script";
            var policyScriptPath = Path.Combine(_settings.BasePath, policyScriptFilename);

            var txBuildCommand = new TxBuildCommand(
                new[] { purchaseRequest.Utxo },
                new[] { 
                    new TxOutput(buyerAddress, buyerOutputUtxoValues), 
                    new TxOutput(sale.ProceedsAddress, profitAddressUtxoValues, IsFeeDeducted:true) },
                tokenMintUtxoValues,
                policyScriptPath,
                metadataJsonPath,
                collection.SlotExpiry);

            var txSubmissionBody = await _txBuilder.BuildTxAsync(txBuildCommand, collection.PolicyId, sale.Id.ToString());

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
    }
}
