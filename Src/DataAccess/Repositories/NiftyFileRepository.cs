using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Supporting;
using MoreLinq.Extensions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyFileRepository
    {
        Task<IEnumerable<NiftyFile>> GetByCollectionIdAsync(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(NiftyFile niftyFile, CancellationToken ct);
        Task UpdateManyAsync(IEnumerable<NiftyFile> niftyFiles, CancellationToken ct);
        Task InsertOneAsync(NiftyFile niftyFile, CancellationToken ct);
        Task InsertManyAsync(IEnumerable<NiftyFile> niftyFiles, CancellationToken ct);
    }

    public class NiftyFileRepository : INiftyFileRepository
    {
        private readonly TableClient _niftyFileClient;

        public NiftyFileRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyFileClient = tableClientFactory.CreateClient(Constants.TableNames.NiftyFile);
        }

        public async Task<IEnumerable<NiftyFile>> GetByCollectionIdAsync(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyFileClient.QueryAsync<NiftyFile>(x => x.PartitionKey == collectionId.ToString());
            return await niftyQuery.GetAllAsync(ct);
        }

        public async Task UpdateOneAsync(NiftyFile niftyFile, CancellationToken ct)
        {
            await _niftyFileClient.UpdateEntityAsync(niftyFile, niftyFile.ETag, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(IEnumerable<NiftyFile> niftyFiles, CancellationToken ct)
        {
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(niftyFiles.Select(nf => new TableTransactionAction(TableTransactionActionType.UpdateMerge, nf)));
            await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }

        public async Task InsertOneAsync(NiftyFile niftyFile, CancellationToken ct)
        {
            await _niftyFileClient.AddEntityAsync(niftyFile, ct);
        }

        public async Task InsertManyAsync(IEnumerable<NiftyFile> niftyFiles, CancellationToken ct)
        {
            foreach (var batch in niftyFiles.Batch(100))
            {
                List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
                addEntitiesBatch.AddRange(batch.Select(nf => new TableTransactionAction(TableTransactionActionType.Add, nf)));
                await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
            }
        }
    }
}
