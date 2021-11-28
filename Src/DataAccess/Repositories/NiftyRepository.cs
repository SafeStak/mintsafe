using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Nifty nifty, CancellationToken ct);
        Task UpdateManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct);
    }

    public class NiftyRepository : INiftyRepository
    {
        private readonly TableClient _niftyClient;
        private readonly INiftyMapper _niftyMapper;

        public NiftyRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyMapper niftyMapper)
        {
            _niftyClient = tableClientFactory.CreateClient(Constants.NiftyTableName);
            _niftyMapper = niftyMapper;
        }

        public async Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<Models.Nifty>(x => x.PartitionKey == collectionId.ToString());
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales.Select(_niftyMapper.Map);
        }

        public async Task UpdateOneAsync(Nifty nifty, CancellationToken ct)
        {
            //TODO assign new guid as id
            var niftyCollectionDto = _niftyMapper.Map(nifty);
            await _niftyClient.UpsertEntityAsync(niftyCollectionDto,TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct)
        {
            //TODO assign new guid as id
            var niftyDtos = nifties.Select(_niftyMapper.Map);
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(niftyDtos.Select(nfc => new TableTransactionAction(TableTransactionActionType.UpsertMerge, nfc)));
            await _niftyClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}