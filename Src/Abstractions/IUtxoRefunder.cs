using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface IUtxoRefunder
    {
        Task<string> ProcessRefundForUtxo(
            Utxo utxo, 
            string signingKeyFilePath, 
            string reason, 
            CancellationToken ct = default);
    }
}