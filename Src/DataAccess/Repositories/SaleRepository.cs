using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;

namespace Mintsafe.DataAccess.Repositories
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct);
    }

    public class SaleRepository : ISaleRepository
    {
        private readonly TableClient _saleClient;

        public SaleRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _saleClient = tableClientFactory.CreateClient("Sales");
        }
        
        public async Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<TableEntity>(x => x.PartitionKey == collectionId.ToString()); //TODO define RowKey & PartitionKey
            var sales = await saleQuery.GetAllAsync(ct);
            return sales.Select(MapTableEntityToSale);
        }

        //TODO mapper class
        private Sale MapTableEntityToSale(TableEntity tableEntity)
        {
            return new Sale();
        }
    }
}
