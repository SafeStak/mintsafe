using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyCollectionRepository
    {
        Task<Models.NiftyCollection?> GetById(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Models.NiftyCollection niftyCollection, CancellationToken ct);
    }

    public class NiftyCollectionRepository : INiftyCollectionRepository
    {
        private readonly TableClient _niftyCollectionClient;

        public NiftyCollectionRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyCollectionClient = tableClientFactory.CreateClient(Constants.NiftyCollectionTableName);
        }

        public async Task<Models.NiftyCollection?> GetById(Guid id, CancellationToken ct)
        {
            var niftyCollectionQuery = _niftyCollectionClient.QueryAsync<Models.NiftyCollection>(x => x.RowKey == id.ToString());
            var sales = await niftyCollectionQuery.GetAllAsync(ct); //TODO GetOneAsync
            return sales.FirstOrDefault();
        }

        public async Task UpdateOneAsync(Models.NiftyCollection niftyCollection, CancellationToken ct)
        {
            await _niftyCollectionClient.UpsertEntityAsync(niftyCollection, TableUpdateMode.Merge, ct);
        }
    }
}
