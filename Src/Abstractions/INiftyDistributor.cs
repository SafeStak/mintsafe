using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface INiftyDistributor
{
    Task<NiftyDistributionResult> DistributeNiftiesForSalePurchase(
        Nifty[] allocatedNfts,
        PurchaseAttempt purchaseRequest,
        SaleContext saleContext,
        NetworkContext networkContext,
        CancellationToken ct = default);
}
