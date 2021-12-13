using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface ISaleAllocationStore
    {
        Task<SaleContext> GetOrRestoreSaleContextAsync(
            CollectionAggregate collectionAggregate, Guid workerId, CancellationToken ct);

        Task<Nifty[]> AllocateNiftiesAsync(
            PurchaseAttempt request, SaleContext context, CancellationToken ct);

        Task ReleaseAllocationAsync(
            IReadOnlyCollection<Nifty> allocated, SaleContext context, CancellationToken ct);
    }
}
