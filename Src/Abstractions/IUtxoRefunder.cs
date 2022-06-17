using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IUtxoRefunder
{
    Task<string> ProcessRefundForUtxo(
        UnspentTransactionOutput utxo,
        SaleContext saleContext,
        NetworkContext networkContext,
        string reason, 
        CancellationToken ct = default);
}
