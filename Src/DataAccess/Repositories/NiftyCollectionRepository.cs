using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyCollectionRepository
    {
        Task<NiftyCollection?> GetById(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(NiftyCollection niftyCollection, CancellationToken ct);
        Task InsertOneAsync(NiftyCollection niftyCollection, CancellationToken ct);
    }

    public class NiftyCollectionRepository : INiftyCollectionRepository
    {
        private readonly TableClient _niftyCollectionClient;

        public NiftyCollectionRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyCollectionClient = tableClientFactory.CreateClient(Constants.TableNames.NiftyCollection);
        }

        public async Task<NiftyCollection?> GetById(Guid id, CancellationToken ct)
        {
            var niftyCollectionQuery = _niftyCollectionClient.QueryAsync<NiftyCollection>(x => x.RowKey == id.ToString());
            var sales = await niftyCollectionQuery.GetAllAsync(ct); //TODO GetOneAsync
            return sales.FirstOrDefault();
        }

        public async Task UpdateOneAsync(NiftyCollection niftyCollection, CancellationToken ct)
        {
            await _niftyCollectionClient.UpdateEntityAsync(niftyCollection, niftyCollection.ETag, TableUpdateMode.Merge, ct);
        }

        public async Task InsertOneAsync(NiftyCollection niftyCollection, CancellationToken ct)
        {
            niftyCollection.RowKey = Guid.NewGuid().ToString();
            await _niftyCollectionClient.AddEntityAsync(niftyCollection, ct);
        }
    }
}
