using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ITokenAllocator
{
    Task<Nifty[]> AllocateTokensForPurchaseAsync(
        NiftySalePurchaseRequest request,
        IList<Nifty> saleAllocatedNfts,
        IList<Nifty> saleMintableNfts,
        NiftySale sale,
        CancellationToken ct = default);
}
