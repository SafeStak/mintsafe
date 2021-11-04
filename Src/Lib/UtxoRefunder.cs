using NiftyLaunchpad.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class UtxoRefunder
    {
        private readonly ITxIoRetriever _txRetriever;
        private readonly ITxSubmitter _txSubmitter;
        private readonly ITxBuilder _txBuilder;

        public UtxoRefunder(
            ITxIoRetriever txRetriever, 
            ITxSubmitter txSubmitter, 
            ITxBuilder txBuilder)
        {
            _txRetriever = txRetriever;
            _txSubmitter = txSubmitter;
            _txBuilder = txBuilder;
        }
        
        public async Task<string> ProcessRefundForUtxo(
            Utxo utxo, string signingKeyFilePath, CancellationToken ct = default)
        {
            var txIo = await _txRetriever.GetTxIoAsync(utxo.TxHash, ct);
            var buyerAddress = txIo.Inputs.First().Address;

            Console.WriteLine($"Processing {utxo.Lovelaces} lovelace refund back to {buyerAddress}");

            var txRefundCommand = new TxBuildCommand(
                new[] { utxo },
                new[] { new TxOutput(buyerAddress, utxo.Values, IsFeeDeducted: true) },
                Array.Empty<UtxoValue>(),
                string.Empty,
                string.Empty,
                0,
                new[] { signingKeyFilePath });
            var submissionPayload = await _txBuilder.BuildTxAsync(txRefundCommand, ct);
            var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct);

            return txHash;
        }
    }
}
