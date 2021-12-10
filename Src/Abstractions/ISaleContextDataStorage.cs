using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface ISaleContextDataStorage
    {
        Task AddAllocationAsync(
            IEnumerable<Nifty> allocated, SaleContext context, CancellationToken ct);
        Task ReleaseAllocationAsync(
            IEnumerable<Nifty> allocated, SaleContext context, CancellationToken ct);
    }
}
