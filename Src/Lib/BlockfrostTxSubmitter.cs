using Mintsafe.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostTxSubmitter : ITxSubmitter
{
    private readonly IBlockfrostClient _blockFrostClient;

    public BlockfrostTxSubmitter(IBlockfrostClient blockFrostClient)
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
    public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        return "51e9b6577ad260c273aee5a3786d6b39cce44fc3c49bf44f395499d34b3814f5";
    }
}
