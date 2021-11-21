using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct);
    }

    public class NiftyRepository : RepositoryBase, INiftyRepository
    {
        private readonly TableClient _niftyClient;

        public NiftyRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _niftyClient = tableClientFactory.CreateClient("Nifty");
        }

        public async Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<TableEntity>(x => x.PartitionKey == collectionId.ToString()); //TODO define RowKey & PartitionKey
            var sales = await GetAllAsync(niftyQuery, ct);
            return sales.Select(MapTableEntityToNifty);
        }

        private Nifty MapTableEntityToNifty(TableEntity tableEntity)
        {
            return new Nifty();
        }
    }
}