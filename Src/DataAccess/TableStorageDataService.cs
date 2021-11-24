using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Composers;
using Mintsafe.DataAccess.Repositories;

namespace Mintsafe.DataAccess
{
    public class TableStorageDataService : INiftyDataService
    {
        private readonly INiftyCollectionRepository _niftyCollectionRepository;
        private readonly INiftyRepository _niftyRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly INiftyFileRepository _niftyFileRepository;

        private readonly ICollectionAggregateComposer _collectionAggregateComposer;

        public TableStorageDataService(INiftyCollectionRepository niftyCollectionRepository, ISaleRepository saleRepository, INiftyRepository niftyRepository, INiftyFileRepository niftyFileRepository, ICollectionAggregateComposer collectionAggregateComposer)
        {
            _niftyCollectionRepository = niftyCollectionRepository ?? throw new ArgumentNullException(nameof(niftyCollectionRepository));
            _niftyRepository = niftyRepository ?? throw new ArgumentNullException(nameof(niftyRepository));
            _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
            _niftyFileRepository = niftyFileRepository ?? throw new ArgumentNullException(nameof(niftyFileRepository));
            _collectionAggregateComposer = collectionAggregateComposer;
        }

        public async Task<CollectionAggregate> GetCollectionAggregateAsync(Guid collectionId, CancellationToken ct = default)
        {
            var niftyCollectionTask = _niftyCollectionRepository.GetById(collectionId, ct);
            var niftyTask = _niftyRepository.GetByCollectionId(collectionId, ct);
            var saleTask = _saleRepository.GetByCollectionId(collectionId, ct);

            //instrumentation and exception handling - ILogger

            await Task.WhenAll(niftyCollectionTask, niftyTask, saleTask);

            var niftyCollection = await niftyCollectionTask;
            var nifties = await niftyTask;
            var sales = await saleTask;

            var niftyFiles = await _niftyFileRepository.GetByNiftyIds(nifties.Select(x => x.Id), ct);

            return _collectionAggregateComposer.Build(niftyCollection, nifties, sales, niftyFiles);
        }
    }
}
