using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyCollectionRepository
    {
        Task<NiftyCollection> GetById(Guid collectionId, CancellationToken ct);
    }

    public class NiftyCollectionRepository : INiftyCollectionRepository
    {
        private readonly TableClient _niftyClient;

        public NiftyCollectionRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyClient = tableClientFactory.CreateClient("NiftyCollection");
        }

        public async Task<NiftyCollection> GetById(Guid id, CancellationToken ct)
        {
            var niftyCollectionQuery = _niftyClient.QueryAsync<TableEntity>(x => x.RowKey == id.ToString()); //TODO define RowKey & PartitionKey
            var sales = await niftyCollectionQuery.GetAllAsync(ct);
            return sales.Select(MapTableEntityToNiftyCollection).FirstOrDefault(); //TODO
        }

        //TODO mapper class
        private NiftyCollection MapTableEntityToNiftyCollection(TableEntity tableEntity)
        {
            return null;
        }
    }
}
