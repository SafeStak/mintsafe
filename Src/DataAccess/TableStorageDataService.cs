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
            //TODO in parallel for performance?
            var niftyCollection = await _niftyCollectionRepository.GetById(collectionId, ct);
            var niftys = await _niftyRepository.GetByCollectionId(collectionId, ct);
            var sales = await _saleRepository.GetByCollectionId(collectionId, ct);

            return new CollectionAggregate(niftyCollection, niftys.ToArray(), sales.ToArray());
        }
    }
}
