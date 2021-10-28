using NiftyLaunchpad.Abstractions;
using System;
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

        public async Task<string> MintAsync(
            Nifty[] nfts, 
            NiftySalePurchaseRequest request, 
            NiftyCollection collection,
            NiftySale sale,
            CancellationToken ct = default)
        {
            // Generate metadata file
            var metadataJsonFileName = $"metadata-{request.Utxo.ShortForm()}.json";
            await _metadataGenerator.GenerateMetadataJsonFile(nfts, collection, metadataJsonFileName, ct);

            // Derive buyer address
            var txIo = await _txRetriever.GetBasicTxAsync(request.Utxo.TxHash);
            var buyerAddress = txIo.Inputs.First().Address;

            // Map UtxoValues for new tokens
            var tokenMintValues = nfts.Select(n => new UtxoValue($"{collection.PolicyId}.{n.AssetName}", 1)).ToArray();
            var tokenOutputUtxoValues = GetTxOutputUtxoValues(tokenMintValues);

            var ttl = _settings.Network == Network.Mainnet
                ? TimeUtil.GetMainnetSlotAt(collection.LockedAt)
                : TimeUtil.GetTestnetSlotAt(collection.LockedAt);

            // Calculate Fees
            var feeCalculationTxRawOutputPath = $"{request.Utxo.ShortForm()}-feecalc.txraw";
            var txBuildCommand = new TxBuildCommand(
                new[] { request.Utxo },
                new[] { 
                    new TxOutput(buyerAddress, tokenOutputUtxoValues), 
                    new TxOutput(sale.SaleAddress, new[] { new UtxoValue("lovelace", request.Utxo.Lovelaces()) }) },
                tokenMintValues,
                $"{collection.PolicyId}.script",
                metadataJsonFileName,
                ttl,
                0,
                feeCalculationTxRawOutputPath);

            var txBody = _txBuilder.BuildTxAsync(txBuildCommand);

            // Calculate Fee

            // Build TxBody 

            var txHash = await _txSubmitter.SubmitTxAsync(Array.Empty<byte>(), ct);

            return txHash;
        }

        private static UtxoValue[] GetTxOutputUtxoValues(UtxoValue[] tokenMintValues)
        {
            var tokenOutputUtxoValues = new UtxoValue[tokenMintValues.Length + 1];
            for (var i = 0; i < tokenMintValues.Length; i++)
            {
                tokenOutputUtxoValues[i] = tokenMintValues[i];
            }
            tokenOutputUtxoValues[tokenMintValues.Length] = new UtxoValue("lovelace", 2000000);
            return tokenOutputUtxoValues;
        }
    }
}
