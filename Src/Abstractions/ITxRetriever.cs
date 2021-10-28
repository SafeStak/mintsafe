using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Abstractions
{
    public interface ITxRetriever
    {
        Task<TxBasic> GetBasicTx(string txHash, CancellationToken ct = default);
    }
}
