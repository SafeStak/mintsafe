using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface INiftyDataService
{
    public Task<ProjectAggregate?> GetCollectionAggregateAsync(Guid collectionId, CancellationToken ct = default);
    public Task<SaleAggregate?> GetSaleAggregateAsync(Guid saleId, CancellationToken ct = default);
    Task InsertCollectionAggregateAsync(ProjectAggregate collectionAggregate, CancellationToken ct = default);
}
