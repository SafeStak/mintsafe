using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ITokenAllocator
{
    Task<Nifty[]> AllocateTokensAsync(
        NiftySalePurchaseRequest request, 
        List<Nifty> allocatedTokens,
        NiftySale sale,
        CancellationToken ct = default);
}
