using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface ITxIoRetriever
    {
        Task<TxIoAggregate> GetTxIoAsync(string txHash, CancellationToken ct = default);
    }
}
