using System.Threading;
using System.Threading.Tasks;

public interface ITxSubmitter
{
    public Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default);
}
