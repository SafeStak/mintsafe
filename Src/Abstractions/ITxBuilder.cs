using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface ITxBuilder
    {
        public Task<byte[]> BuildTxAsync(
            TxBuildCommand buildCommand,
            CancellationToken ct = default);
    }
}