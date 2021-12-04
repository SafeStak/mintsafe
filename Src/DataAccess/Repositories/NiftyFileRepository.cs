using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyFileRepository
    {
        Task<IEnumerable<NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Guid collectionId, NiftyFile niftyFile, CancellationToken ct);
        Task UpdateManyAsync(Guid collectionId, IEnumerable<NiftyFile> niftyFiles, CancellationToken ct);
        Task InsertOneAsync(Guid collectionId, NiftyFile niftyFile, CancellationToken ct);
        Task InsertManyAsync(Guid collectionId, IEnumerable<NiftyFile> niftyFiles, CancellationToken ct);
    }

    public class NiftyFileRepository : INiftyFileRepository
    {
        private readonly TableClient _niftyFileClient;

        public NiftyFileRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyFileClient = tableClientFactory.CreateClient(Constants.TableNames.NiftyFile);
        }

        public async Task<IEnumerable<NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyFileClient.QueryAsync<NiftyFile>(x => x.PartitionKey == collectionId.ToString());
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales;
        }

        public async Task UpdateOneAsync(Guid collectionId, NiftyFile niftyFile, CancellationToken ct)
        {
            niftyFile.PartitionKey = collectionId.ToString();
            await _niftyFileClient.UpdateEntityAsync(niftyFile, niftyFile.ETag, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(Guid collectionId, IEnumerable<NiftyFile> niftyFiles, CancellationToken ct)
        {
            foreach (var niftyFile in niftyFiles)
            {
                niftyFile.PartitionKey = collectionId.ToString(); //TODO?
            }

            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(niftyFiles.Select(nf => new TableTransactionAction(TableTransactionActionType.UpdateMerge, nf)));
            await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }

        public async Task InsertOneAsync(Guid collectionId, NiftyFile niftyFile, CancellationToken ct)
        {
            niftyFile.RowKey = Guid.NewGuid().ToString();
            niftyFile.PartitionKey = collectionId.ToString(); //TODO
            await _niftyFileClient.AddEntityAsync(niftyFile, ct);
        }

        public async Task InsertManyAsync(Guid collectionId, IEnumerable<NiftyFile> niftyFiles, CancellationToken ct)
        {
            var files = niftyFiles.ToList();
            foreach (var niftyFile in files)
            {
                niftyFile.RowKey = Guid.NewGuid().ToString();
                niftyFile.PartitionKey = collectionId.ToString(); //TODO?
            }

            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(files.Select(nf => new TableTransactionAction(TableTransactionActionType.Add, nf)));
            await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}
