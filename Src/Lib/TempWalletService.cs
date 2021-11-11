using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class TempWalletService
    {
        private readonly ILogger<TempWalletService> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly IUtxoRetriever _utxoRetriever;
        private readonly IMetadataGenerator _metadataGenerator;
        private readonly ITxSubmitter _txSubmitter;
        private readonly ITxBuilder _txBuilder;

        public TempWalletService(
            ILogger<TempWalletService> logger,
            MintsafeSaleWorkerSettings settings,
            IUtxoRetriever utxoRetriever,
            IMetadataGenerator metadataGenerator,
            ITxBuilder txBuilder,
            ITxSubmitter txSubmitter)
        {
            _logger = logger;
            _settings = settings;
            _utxoRetriever = utxoRetriever;
            _metadataGenerator = metadataGenerator;
            _txBuilder = txBuilder;
            _txSubmitter = txSubmitter;
        }

        public async Task<string> SendAllConsolidatedUtxos(
            string sourceAddress,
            string destinationAddress,
            string[] message,
            string signingKeyFilePath,
            CancellationToken ct = default)
        {
            var paymentId = Guid.NewGuid();

            // Generate payment message metadata 
            var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct);

            var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct);
            var combinedUtxoValues = utxosAtSourceAddress.SelectMany(u => u.Values)
                .GroupBy(uv => uv.Unit)
                .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
                .ToArray();

            var sendAllConsolidatedUtxosCommand = new TxBuildCommand(
                utxosAtSourceAddress,
                new[] { 
                    new TxOutput(destinationAddress, combinedUtxoValues, IsFeeDeducted: true) },
                Mint: Array.Empty<Value>(),
                MintingScriptPath: string.Empty,
                MetadataJsonPath: metadataJsonPath,
                TtlSlot: 0,
                new[] { signingKeyFilePath });
            var submissionPayload = await _txBuilder.BuildTxAsync(sendAllConsolidatedUtxosCommand, ct);
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);

            return txHash;
        }

        public async Task<string> SendValuesConsolidated(
            string sourceAddress,
            string destinationAddress,
            Value[] values,
            string[] message,
            string signingKeyFilePath,
            CancellationToken ct = default)
        {
            var paymentId = Guid.NewGuid();

            var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct);

            // Validate
            var combinedAssetValues = utxosAtSourceAddress.SelectMany(u => u.Values)
                .GroupBy(uv => uv.Unit)
                .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
                .ToArray();
            foreach (var valueToSend in values)
            {
                var combinedUnitValue = combinedAssetValues.FirstOrDefault(u => u.Unit == valueToSend.Unit);
                if (combinedUnitValue == null)
                    throw new ArgumentException($"{nameof(values)} utxo does not exist at source address");
                if (combinedUnitValue.Quantity < valueToSend.Quantity)
                    throw new ArgumentException($"{nameof(values)} quantity in source address insufficient for payment");
            }

            // Generate payment message metadata 
            var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct);

            var changeValues = SubtractValues(combinedAssetValues, values);
            var txRefundCommand = new TxBuildCommand(
                utxosAtSourceAddress,
                new[] {
                    new TxOutput(destinationAddress, values),
                    new TxOutput(sourceAddress, changeValues, IsFeeDeducted: true),
                },
                Mint: Array.Empty<Value>(),
                MintingScriptPath: string.Empty,
                MetadataJsonPath: metadataJsonPath,
                TtlSlot: 0,
                new[] { signingKeyFilePath });
            var submissionPayload = await _txBuilder.BuildTxAsync(txRefundCommand, ct);
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);

            return txHash;
        }

        private static Value[] SubtractValues(Value[] lhs, Value[] rhs)
        {
            static Value SubtractValue(Value valueLhs, Value? valueRhs)
            {
                if (valueRhs == null)
                {
                    return valueLhs;
                }
                return new Value(valueLhs.Unit, valueLhs.Quantity - valueRhs.Quantity);
            };

            var diff = lhs
                .Select(
                    lv => SubtractValue(lv, rhs.FirstOrDefault(rv => rv.Unit == lv.Unit)))
                .ToArray();

            return diff;
        }
    }
}
