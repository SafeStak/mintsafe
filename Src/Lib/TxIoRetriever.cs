using NiftyLaunchpad.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxIoRetriever : ITxIoRetriever
    {
        private readonly BlockfrostClient _blockFrostClient;

        public TxIoRetriever(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<TxIoAggregate> GetTxIoAsync(string txHash, CancellationToken ct = default)
        {
            var bfResult = await _blockFrostClient.GetTransactionAsync(txHash, ct).ConfigureAwait(false);

            return bfResult;
        }
    }

    public class FakeTxIoRetriever : ITxIoRetriever
    {
        private readonly BlockfrostClient _blockFrostClient;

        public FakeTxIoRetriever(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<TxIoAggregate> GetTxIoAsync(string txHash, CancellationToken ct = default)
        {
            await Task.Delay(100, ct);

            return new TxIoAggregate(
                txHash,
                Inputs: new[] { new TxIo("addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x", 0, new[] { new UtxoValue("lovelace", 10200000) }) },
                Outputs: new[] { new TxIo("addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x", 0, new[] { new UtxoValue("lovelace", 10000000) }) });
        }
    }
}
