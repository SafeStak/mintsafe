using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface INiftyDataService
{
    public Task<CollectionAggregate> GetCollectionAggregateAsync(Guid collectionId, CancellationToken ct = default);
    Task InsertCollectionAggregateAsync(CollectionAggregate collectionAggregate, CancellationToken ct = default);
}
