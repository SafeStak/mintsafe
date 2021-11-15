using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IUtxoRetriever
{
    Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default);
}
