using System.Threading;
using System.Threading.Tasks;

public interface ITxBuilder
{
    public Task<byte[]> BuildTxAsync(
        TxBuildCommand buildCommand, CancellationToken ct = default);
}
