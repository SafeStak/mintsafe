using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Models.Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Models.Nifty nifty, CancellationToken ct);
        Task UpdateManyAsync(IEnumerable<Models.Nifty> nifties, CancellationToken ct);
    }

    public class NiftyRepository : INiftyRepository
    {
        private readonly TableClient _niftyClient;

        public NiftyRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyClient = tableClientFactory.CreateClient(Constants.NiftyTableName);
        }

        public async Task<IEnumerable<Models.Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<Models.Nifty>(x => x.PartitionKey == collectionId.ToString());
            return await niftyQuery.GetAllAsync(ct);
        }

        public async Task UpdateOneAsync(Models.Nifty nifty, CancellationToken ct)
        {
            await _niftyClient.UpsertEntityAsync(nifty, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(IEnumerable<Models.Nifty> nifties, CancellationToken ct)
        {
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(nifties.Select(nfc => new TableTransactionAction(TableTransactionActionType.UpsertMerge, nfc)));
            await _niftyClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}