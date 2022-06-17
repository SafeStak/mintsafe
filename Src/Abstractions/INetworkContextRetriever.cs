using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface INetworkContextRetriever
{
    Task<NetworkContext> GetNetworkContext(CancellationToken ct = default);
}
