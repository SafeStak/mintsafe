using System.Threading;
using System.Threading.Tasks;

public interface ITokenAllocator
{
    Task<Nifty[]> AllocateTokensAsync(
        NiftySalePurchaseRequest request, 
        CancellationToken ct = default);
}
