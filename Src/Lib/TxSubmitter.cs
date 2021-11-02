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

    public class FakeTxSubmitter : ITxSubmitter
    {
        private readonly BlockfrostClient _blockFrostClient;

        public FakeTxSubmitter(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            await Task.Delay(100);
            return "51e9b6577ad260c273aee5a3786d6b39cce44fc3c49bf44f395499d34b3814f5";
        }
    }
}
