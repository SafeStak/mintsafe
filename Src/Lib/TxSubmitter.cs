using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxSubmitter : ITxSubmitter
    {
        private readonly BlockfrostClient _blockFrostClient;

        public TxSubmitter(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            var txHash = await _blockFrostClient.SubmitTransactionAsync(txSignedBinary, ct);

            return txHash;
        }
    }
}
