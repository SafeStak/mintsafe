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
            NiftySalePurchaseRequest request, 
            NiftyCollection collection,
            NiftySale sale,
            CancellationToken ct = default)
        {
            // Generate metadata file
            var metadataJsonFileName = $"metadata-{request.Utxo.ShortForm()}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);

            await _metadataGenerator.GenerateMetadataJsonFile(nfts, collection, metadataJsonPath, ct);

            // Derive buyer address after getting source UTxO details from BF
            var txIo = await _txRetriever.GetBasicTxAsync(request.Utxo.TxHash);
            var buyerAddress = txIo.Inputs.First().Address;

            // Map UtxoValues for new tokens
            long buyerLovelacesReturned = 2000000;
            var tokenMintValues = nfts.Select(n => new UtxoValue($"{collection.PolicyId}.{n.AssetName}", 1)).ToArray();
            var buyerOutputUtxoValues = GetTxOutputUtxoValues(tokenMintValues, buyerLovelacesReturned);
            var depositAddressLovelaces = request.Utxo.Lovelaces() - buyerLovelacesReturned;

            //var ttl = _settings.Network == Network.Mainnet
            //    ? TimeUtil.GetMainnetSlotAt(collection.LockedAt)
            //    : TimeUtil.GetTestnetSlotAt(collection.LockedAt);

            var policyScriptFilename = $"{collection.PolicyId}.script";
            var policyScriptPath = Path.Combine(_settings.BasePath, policyScriptFilename);

            var txBuildCommand = new TxBuildCommand(
                new[] { request.Utxo },
                new[] { 
                    new TxOutput(buyerAddress, buyerOutputUtxoValues), 
                    new TxOutput(sale.DepositAddress, new[] { new UtxoValue("lovelace", depositAddressLovelaces) }) },
                tokenMintValues,
                policyScriptPath,
                metadataJsonPath,
                collection.SlotExpiry);

            var txSubmissionBody = await _txBuilder.BuildTxAsync(txBuildCommand, collection.PolicyId, sale.Id.ToString());

            var txHash = await _txSubmitter.SubmitTxAsync(txSubmissionBody, ct);

            return txHash;
        }

        private static UtxoValue[] GetTxOutputUtxoValues(
            UtxoValue[] tokenMintValues, long minLovelace = 2000000)
        {
            var tokenOutputUtxoValues = new UtxoValue[tokenMintValues.Length + 1];
            for (var i = 0; i < tokenMintValues.Length; i++)
            {
                tokenOutputUtxoValues[i] = tokenMintValues[i];
            }
            tokenOutputUtxoValues[tokenMintValues.Length] = new UtxoValue("lovelace", minLovelace);
            return tokenOutputUtxoValues;
        }
    }
}
