﻿using NiftyLaunchpad.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class UtxoRefunder
    {
        private readonly NiftyLaunchpadSettings _settings;
        private readonly ITxIoRetriever _txRetriever;
        private readonly IMetadataGenerator _metadataGenerator;
        private readonly ITxSubmitter _txSubmitter;
        private readonly ITxBuilder _txBuilder;

        public UtxoRefunder(
            NiftyLaunchpadSettings settings,
            ITxIoRetriever txRetriever,
            IMetadataGenerator metadataGenerator,
            ITxBuilder txBuilder,
            ITxSubmitter txSubmitter)
        {
            _settings = settings;
            _txRetriever = txRetriever;
            _metadataGenerator = metadataGenerator;
            _txBuilder = txBuilder;
            _txSubmitter = txSubmitter;
        }

        public async Task<string> ProcessRefundForUtxo(
            Utxo utxo, string signingKeyFilePath, string reason, CancellationToken ct = default)
        {
            var txIo = await _txRetriever.GetTxIoAsync(utxo.TxHash, ct);
            var buyerAddress = txIo.Inputs.First().Address;

            // Generate refund message metadata 
            var metadataJsonFileName = $"metadata-refund-{utxo}.json";
            var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
            var message = new[] {
                $"mint{{SAFE}} refund",
                utxo.TxHash,
                $"#{utxo.OutputIndex}",
                reason
            };
            await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct);

            var txRefundCommand = new TxBuildCommand(
                new[] { utxo },
                new[] { new TxOutput(buyerAddress, utxo.Values, IsFeeDeducted: true) },
                Array.Empty<UtxoValue>(),
                string.Empty,
                metadataJsonPath,
                0,
                new[] { signingKeyFilePath });
            var submissionPayload = await _txBuilder.BuildTxAsync(txRefundCommand, ct);
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);

            return txHash;
        }
    }
}
