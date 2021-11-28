using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Nifty nifty, CancellationToken ct);
        Task UpdateManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct);
        Task InsertOneAsync(Nifty nifty, CancellationToken ct);
        Task InsertManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct);
    }

    public class NiftyRepository : INiftyRepository
    {
        private readonly TableClient _niftyClient;

        public NiftyRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyClient = tableClientFactory.CreateClient(Constants.NiftyTableName);
        }

        public async Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<Nifty>(x => x.PartitionKey == collectionId.ToString());
            return await niftyQuery.GetAllAsync(ct);
        }

        public async Task UpdateOneAsync(Nifty nifty, CancellationToken ct)
        {
            await _niftyClient.UpsertEntityAsync(nifty, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct)
        {
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(nifties.Select(nfc => new TableTransactionAction(TableTransactionActionType.UpdateMerge, nfc)));
            await _niftyClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }

        public async Task InsertOneAsync(Nifty nifty, CancellationToken ct)
        {
            nifty.RowKey = Guid.NewGuid().ToString();
            await _niftyClient.AddEntityAsync(nifty, ct);
        }

        public async Task InsertManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct)
        {
            foreach (var nifty in nifties)
            {
                nifty.RowKey = Guid.NewGuid().ToString();
            }

            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(nifties.Select(nfc => new TableTransactionAction(TableTransactionActionType.Add, nfc)));
            await _niftyClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}