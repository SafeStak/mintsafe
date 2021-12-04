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
                var niftyCollectionTask = _niftyCollectionRepository.GetById(collectionId, ct);
                var niftyTask = _niftyRepository.GetByCollectionId(collectionId, ct);
                var saleTask = _saleRepository.GetByCollectionId(collectionId, ct);
                var niftyFileTask = _niftyFileRepository.GetByCollectionId(collectionId, ct);

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

        //WIP quick hack
        public async Task InsertCollectionAggregateAsync(CollectionAggregate collectionAggregate, CancellationToken ct = default)
        {
            var collectionId = collectionAggregate.Collection.Id;

            var niftyCollection = NiftyCollectionMapper.Map(collectionAggregate.Collection);
            var nifties = collectionAggregate.Tokens.Select(NiftyMapper.Map);
            var sales = collectionAggregate.ActiveSales.Select(SaleMapper.Map);
            var files = collectionAggregate.Tokens.SelectMany(x => x.Files.Select(f => NiftyFileMapper.Map(collectionId, f)));

            try
            {
                await _niftyCollectionRepository.InsertOneAsync(niftyCollection, ct);
                await _niftyRepository.InsertManyAsync(nifties, ct);
                await _saleRepository.InsertManyAsync(sales, ct);
                await _niftyFileRepository.InsertManyAsync(files, ct);
            }
            catch (Exception e)
            {
                _logger.LogError(666, e, "Failed to insert");
                throw;
            }
        }
    }
}
