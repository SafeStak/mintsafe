using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TempPaymentService
    {
        private readonly NiftyLaunchpadSettings _settings;
        private readonly IUtxoRetriever _utxoRetriever;
        private readonly IMetadataGenerator _metadataGenerator;
        private readonly ITxSubmitter _txSubmitter;
        private readonly ITxBuilder _txBuilder;

        public TempPaymentService(
            NiftyLaunchpadSettings settings,
            IUtxoRetriever utxoRetriever,
            IMetadataGenerator metadataGenerator,
            ITxBuilder txBuilder,
            ITxSubmitter txSubmitter)
        {
            _settings = settings;
            _utxoRetriever = utxoRetriever;
            _metadataGenerator = metadataGenerator;
            _txBuilder = txBuilder;
            _txSubmitter = txSubmitter;
        }

        public async Task<string> SendConsolidatedUtxos(
            string fromAddress,
            string toAddress,
            string[] message,
            string signingKeyFilePath,
            CancellationToken ct = default)
        {
            var paymentId = Guid.NewGuid();

            // Generate payment message metadata 
            var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct);

            var utxos = await _utxoRetriever.GetUtxosAtAddressAsync(fromAddress, ct);
            var combinedUtxoValues = utxos.SelectMany(u => u.Values)
                .GroupBy(uv => uv.Unit)
                .Select(uvg => new UtxoValue(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
                .ToArray();

            var txRefundCommand = new TxBuildCommand(
                utxos,
                new[] { 
                    new TxOutput(toAddress, combinedUtxoValues, IsFeeDeducted: true) },
                Mint: Array.Empty<UtxoValue>(),
                MintingScriptPath: string.Empty,
                MetadataJsonPath: metadataJsonPath,
                TtlSlot: 0,
                new[] { signingKeyFilePath });
            var submissionPayload = await _txBuilder.BuildTxAsync(txRefundCommand, ct);
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);

            return txHash;
        }
    }
}
