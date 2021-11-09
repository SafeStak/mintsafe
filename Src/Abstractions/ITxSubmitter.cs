using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface ITxSubmitter
    {
        public Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default);
    }
}