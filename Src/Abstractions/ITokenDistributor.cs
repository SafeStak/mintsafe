using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface ITokenDistributor
    {
        Task<string> DistributeNiftiesForSalePurchase(
            Nifty[] nfts, 
            PurchaseAttempt purchaseRequest, 
            NiftyCollection collection, 
            Sale sale, 
            CancellationToken ct = default);
    }
}