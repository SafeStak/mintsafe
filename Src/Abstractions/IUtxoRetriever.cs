using System.Threading;
using System.Threading.Tasks;

public interface IUtxoRetriever
{
    Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default);
}
