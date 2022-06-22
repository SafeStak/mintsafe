using Mintsafe.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IMintingKeychainRetriever
{
    Task<MintingKeyChain> GetMintingKeyChainAsync(
        SaleContext saleContext, 
        CancellationToken ct = default);
}