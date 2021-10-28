using NiftyLaunchpad.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxRetriever : ITxRetriever
    {
        private readonly BlockfrostClient _blockFrostClient;

        public TxRetriever(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<TxBasic> GetBasicTxAsync(string txHash, CancellationToken ct = default)
        {
            var bfResult = await _blockFrostClient.GetTransactionAsync(txHash, ct).ConfigureAwait(false);

            return bfResult;
        }
    }
}
