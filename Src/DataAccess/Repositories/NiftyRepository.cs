using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct);
    }

    public class NiftyRepository : INiftyRepository
    {
        private readonly TableClient _niftyClient;

        public NiftyRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyClient = tableClientFactory.CreateClient("Nifty");
        }

        public async Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<TableEntity>(x => x.PartitionKey == collectionId.ToString()); //TODO define RowKey & PartitionKey
            var sales = await niftyQuery.GetAllAsync(ct);
            return sales.Select(MapTableEntityToNifty);
        }

        //TODO mapper class
        private Nifty MapTableEntityToNifty(TableEntity tableEntity)
        {
            return null;
        }
    }
}