using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Repositories;

namespace Mintsafe.DataAccess
{
    public class TableStorageDataService : INiftyDataService
    {
        private readonly INiftyCollectionRepository _niftyCollectionRepository;
        private readonly INiftyRepository _niftyRepository;
        private readonly ISaleRepository _saleRepository;

        public TableStorageDataService(INiftyCollectionRepository niftyCollectionRepository, ISaleRepository saleRepository, INiftyRepository niftyRepository)
        {
            _niftyCollectionRepository = niftyCollectionRepository ?? throw new ArgumentNullException(nameof(niftyCollectionRepository));
            _niftyRepository = niftyRepository ?? throw new ArgumentNullException(nameof(niftyRepository));
            _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
        }

        public async Task<CollectionAggregate> GetCollectionAggregateAsync(Guid collectionId, CancellationToken ct = default)
        {
            var niftyCollectionTask = _niftyCollectionRepository.GetById(collectionId, ct);
            var niftyTask = _niftyRepository.GetByCollectionId(collectionId, ct);
            var saleTask = _saleRepository.GetByCollectionId(collectionId, ct); //TODO get active only

            //instrumentation and exception handling - ILogger

            await Task.WhenAll(niftyCollectionTask, niftyTask, saleTask);

            var niftyCollection = await niftyCollectionTask;
            var nifties = (await niftyTask).ToArray();
            var sales = (await saleTask).ToArray();

            //var fileTask = _niftyFileRepository.GetByNiftyIds(nifties.Select(x => x.Id), ct);

            //TODO CollectionAggregate composer

            return new CollectionAggregate(niftyCollection, nifties, sales);
        }
    }
}
