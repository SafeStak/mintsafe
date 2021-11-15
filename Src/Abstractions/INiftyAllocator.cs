using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface INiftyAllocator
{
    Task<Nifty[]> AllocateTokensForPurchaseAsync(
        PurchaseAttempt request,
        IList<Nifty> saleAllocatedNfts,
        IList<Nifty> saleMintableNfts,
        Sale sale,
        CancellationToken ct = default);
}
