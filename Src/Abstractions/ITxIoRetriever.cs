using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface ITxIoRetriever
{
    Task<TxInfo> GetTxIoAsync(string txHash, CancellationToken ct = default);
}
