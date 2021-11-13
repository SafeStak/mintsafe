using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public interface IYoloWalletService
    {
        Task<string> SendAllAsync(
            string sourceAddress,
            string destinationAddress,
            string[] message,
            string signingKeyCborHex,
            CancellationToken ct = default);

        Task<string> SendValuesAsync(
            string sourceAddress,
            string destinationAddress,
            Value[] values,
            string[] message,
            string signingKeyCborHex,
            CancellationToken ct = default);
    }

    public class YoloWalletService : IYoloWalletService
    {
        private readonly ILogger<YoloWalletService> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly IUtxoRetriever _utxoRetriever;
        private readonly IMetadataGenerator _metadataGenerator;
        private readonly ITxSubmitter _txSubmitter;
        private readonly ITxBuilder _txBuilder;

        public YoloWalletService(
            ILogger<YoloWalletService> logger,
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

        public async Task<string> SendAllAsync(
            string sourceAddress,
            string destinationAddress,
            string[] message,
            string signingKeyFilePath,
            CancellationToken ct = default)
        {
            var paymentId = Guid.NewGuid();

            var sw = Stopwatch.StartNew();
            var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct);
            _logger.LogInformation($"{nameof(_utxoRetriever.GetUtxosAtAddressAsync)} completed with {utxosAtSourceAddress.Length} after {sw.ElapsedMilliseconds}ms");

            var combinedUtxoValues = utxosAtSourceAddress.SelectMany(u => u.Values)
                .GroupBy(uv => uv.Unit)
                .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
                .ToArray();

            // Generate payment message metadata 
            sw.Restart();
            var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct);
            _logger.LogInformation($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

            var sendAllConsolidatedUtxosCommand = new TxBuildCommand(
                utxosAtSourceAddress,
                new[] {
                    new TxOutput(destinationAddress, combinedUtxoValues, IsFeeDeducted: true) },
                Mint: Array.Empty<Value>(),
                MintingScriptPath: string.Empty,
                MetadataJsonPath: metadataJsonPath,
                TtlSlot: 0,
                new[] { signingKeyFilePath });

            sw.Restart();
            var submissionPayload = await _txBuilder.BuildTxAsync(sendAllConsolidatedUtxosCommand, ct);
            _logger.LogInformation($"{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);
            _logger.LogInformation($"{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

            return txHash;
        }

        public async Task<string> SendValuesAsync(
            string sourceAddress,
            string destinationAddress,
            Value[] values,
            string[] message,
            string signingKeyFilePath,
            CancellationToken ct = default)
        {
            var paymentId = Guid.NewGuid();

            var sw = Stopwatch.StartNew();
            var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct);
            _logger.LogInformation($"{nameof(_utxoRetriever.GetUtxosAtAddressAsync)} completed with {utxosAtSourceAddress.Length} after {sw.ElapsedMilliseconds}ms");

            // Validate
            var combinedAssetValues = utxosAtSourceAddress.SelectMany(u => u.Values)
                .GroupBy(uv => uv.Unit)
                .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
                .ToArray();
            foreach (var valueToSend in values)
            {
                var combinedUnitValue = combinedAssetValues.FirstOrDefault(u => u.Unit == valueToSend.Unit);
                if (combinedUnitValue == default)
                    throw new ArgumentException($"{nameof(values)} utxo does not exist at source address");
                if (combinedUnitValue.Quantity < valueToSend.Quantity)
                    throw new ArgumentException($"{nameof(values)} quantity in source address insufficient for payment");
            }

            // Generate payment message metadata 
            sw.Restart();
            var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct);
            _logger.LogInformation($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

            // Determine change and build Tx
            var changeValues = SubtractValues(combinedAssetValues, values);
            var txBuildCommand = new TxBuildCommand(
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

            sw.Restart();
            var submissionPayload = await _txBuilder.BuildTxAsync(txBuildCommand, ct);
            _logger.LogInformation($"{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);
            _logger.LogInformation($"{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

            return txHash;
        }

        private static Value[] SubtractValues(
            Value[] lhsValues, Value[] rhsValues)
        {
            static Value SubtractSingleValue(Value lhsValue, Value rhsValue)
            {
                return rhsValue == default
                    ? lhsValue
                    : new Value(lhsValue.Unit, lhsValue.Quantity - rhsValue.Quantity);
            };

            var diff = lhsValues
                .Select(lv => SubtractSingleValue(lv, rhsValues.FirstOrDefault(rv => rv.Unit == lv.Unit)))
                .ToArray();

            return diff;
        }
    }
}
