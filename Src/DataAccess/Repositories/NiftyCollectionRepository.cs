using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyCollectionRepository
    {
        Task<NiftyCollection> GetById(Guid collectionId, CancellationToken ct);
    }

    public class NiftyCollectionRepository : RepositoryBase, INiftyCollectionRepository
    {
        private readonly TableClient _niftyClient;

        public NiftyCollectionRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyClient = tableClientFactory.CreateClient("NiftyCollection");
        }

        public async Task<NiftyCollection> GetById(Guid id, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<TableEntity>(x => x.RowKey == id.ToString()); //TODO define RowKey & PartitionKey
            var sales = await GetAllAsync(niftyQuery, ct);
            return sales.Select(MapTableEntityToNiftyCollection).FirstOrDefault(); //TODO
        }

        private NiftyCollection MapTableEntityToNiftyCollection(TableEntity tableEntity)
        {
            return new NiftyCollection();
        }
    }
}
