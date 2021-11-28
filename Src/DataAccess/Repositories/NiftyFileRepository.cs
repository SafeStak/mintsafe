using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyFileRepository
    {
        Task<IEnumerable<Models.NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Guid collectionId, Models.NiftyFile niftyFile, CancellationToken ct);
        Task UpdateManyAsync(Guid collectionId, IEnumerable<Models.NiftyFile> niftyFiles, CancellationToken ct);
    }

    public class NiftyFileRepository : INiftyFileRepository
    {
        private readonly TableClient _niftyFileClient;

        public NiftyFileRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyFileClient = tableClientFactory.CreateClient(Constants.NiftyFileTableName);
        }

        public async Task<IEnumerable<Models.NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyFileClient.QueryAsync<Models.NiftyFile>(x => x.PartitionKey == collectionId.ToString());
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales;
        }

        public async Task UpdateOneAsync(Guid collectionId, Models.NiftyFile niftyFile, CancellationToken ct)
        {
            niftyFile.PartitionKey = collectionId.ToString(); //TODO?
            await _niftyFileClient.UpsertEntityAsync(niftyFile, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(Guid collectionId, IEnumerable<Models.NiftyFile> niftyFiles, CancellationToken ct)
        {
            foreach (var niftyFile in niftyFiles)
            {
                niftyFile.PartitionKey = collectionId.ToString(); //TODO?
            }

            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(niftyFiles.Select(nf => new TableTransactionAction(TableTransactionActionType.Add, nf)));
            await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}
