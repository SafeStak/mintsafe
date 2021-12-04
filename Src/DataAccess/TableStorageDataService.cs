using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Composers;
using Mintsafe.DataAccess.Mappers;
using Mintsafe.DataAccess.Repositories;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess
{
    public class TableStorageDataService : INiftyDataService
    {
        private readonly INiftyCollectionRepository _niftyCollectionRepository;
        private readonly INiftyRepository _niftyRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly INiftyFileRepository _niftyFileRepository;

        private readonly ICollectionAggregateComposer _collectionAggregateComposer;

        private readonly ILogger<TableStorageDataService> _logger;

        public TableStorageDataService(INiftyCollectionRepository niftyCollectionRepository, ISaleRepository saleRepository, INiftyRepository niftyRepository, INiftyFileRepository niftyFileRepository, ICollectionAggregateComposer collectionAggregateComposer, ILogger<TableStorageDataService> logger)
        {
            _niftyCollectionRepository = niftyCollectionRepository ?? throw new ArgumentNullException(nameof(niftyCollectionRepository));
            _niftyRepository = niftyRepository ?? throw new ArgumentNullException(nameof(niftyRepository));
            _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
            _niftyFileRepository = niftyFileRepository ?? throw new ArgumentNullException(nameof(niftyFileRepository));
            _collectionAggregateComposer = collectionAggregateComposer ?? throw new ArgumentNullException(nameof(collectionAggregateComposer));
            _logger = logger ?? throw new NullReferenceException(nameof(logger));
        }

        public async Task<CollectionAggregate> GetCollectionAggregateAsync(Guid collectionId, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            Models.NiftyCollection? niftyCollection;
            IList<Models.Nifty> nifties;
            IEnumerable<Models.Sale> sales;
            IEnumerable<Models.NiftyFile> niftyFiles;

            try
            {
                var niftyCollectionTask = _niftyCollectionRepository.GetByIdAsync(collectionId, ct);
                var niftyTask = _niftyRepository.GetByCollectionIdAsync(collectionId, ct);
                var saleTask = _saleRepository.GetByCollectionIdAsync(collectionId, ct);
                var niftyFileTask = _niftyFileRepository.GetByCollectionIdAsync(collectionId, ct);

                await Task.WhenAll(niftyCollectionTask, niftyTask, saleTask, niftyFileTask);

                niftyCollection = await niftyCollectionTask;
                nifties = (await niftyTask).ToList();
                sales = await saleTask;
                niftyFiles = await niftyFileTask;

                _logger.LogInformation($"Finished getting all entities for collectionId: {collectionId} from table storage after {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                _logger.LogError(Constants.EventIds.FailedToRetrieve, e, $"Failed to retrieve entities from table storage for collectionId: {collectionId}");
                throw;
            }

            return _collectionAggregateComposer.Build(niftyCollection, nifties, sales, niftyFiles);
        }

        public async Task InsertCollectionAggregateAsync(CollectionAggregate collectionAggregate, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            var collectionId = collectionAggregate.Collection.Id;

            var niftyCollection = NiftyCollectionMapper.Map(collectionAggregate.Collection);
            var nifties = collectionAggregate.Tokens.Select(NiftyMapper.Map);
            var sales = collectionAggregate.ActiveSales.Select(SaleMapper.Map);
            var files = collectionAggregate.Tokens.SelectMany(x => x.Files.Select(f => NiftyFileMapper.Map(collectionId, f)));

            try
            {
                var niftyCollectionTask = _niftyCollectionRepository.InsertOneAsync(niftyCollection, ct);
                var niftyTask = _niftyRepository.InsertManyAsync(nifties, ct);
                var saleTask = _saleRepository.InsertManyAsync(sales, ct);
                var niftyFileTask = _niftyFileRepository.InsertManyAsync(files, ct);

                await Task.WhenAll(niftyCollectionTask, niftyTask, saleTask, niftyFileTask);

                _logger.LogInformation($"Inserted all entities for collectionId: {collectionId} into table storage after {sw.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                _logger.LogError(Constants.EventIds.FailedToInsert, e, $"Failed to insert all entities for collectionId: {collectionId}");
                throw;
            }
        }
    }
}
