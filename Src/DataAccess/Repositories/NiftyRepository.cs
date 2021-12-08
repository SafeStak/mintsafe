using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Supporting;
using MoreLinq.Extensions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Nifty>> GetByCollectionIdAsync(Guid collectionId, CancellationToken ct);
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
            _niftyClient = tableClientFactory.CreateClient(Constants.TableNames.Nifty);
        }

        public async Task<IEnumerable<Nifty>> GetByCollectionIdAsync(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<Nifty>(x => x.PartitionKey == collectionId.ToString());
            return await niftyQuery.GetAllAsync(ct);
        }

        public async Task UpdateOneAsync(Nifty nifty, CancellationToken ct)
        {
            await _niftyClient.UpdateEntityAsync(nifty, nifty.ETag, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct)
        {
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(nifties.Select(nfc => new TableTransactionAction(TableTransactionActionType.UpdateMerge, nfc)));
            await _niftyClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }

        public async Task InsertOneAsync(Nifty nifty, CancellationToken ct)
        {
            await _niftyClient.AddEntityAsync(nifty, ct);
        }

        public async Task InsertManyAsync(IEnumerable<Nifty> nifties, CancellationToken ct)
        {
            foreach (var batch in nifties.Batch(100))
            {
                List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
                addEntitiesBatch.AddRange(batch.Select(nfc => new TableTransactionAction(TableTransactionActionType.Add, nfc)));
                await _niftyClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
            }
        }
    }
}