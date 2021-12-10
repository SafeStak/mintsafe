using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface INiftyAllocator
{
    Task<Nifty[]> AllocateNiftiesForPurchaseAsync(
        PurchaseAttempt request,
        SaleContext saleContext,
        CancellationToken ct = default);
}
