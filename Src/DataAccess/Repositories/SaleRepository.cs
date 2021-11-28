using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Models.Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Models.Sale sale, CancellationToken ct);
    }

    public class SaleRepository : ISaleRepository
    {
        private readonly TableClient _saleClient;

        public SaleRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _saleClient = tableClientFactory.CreateClient(Constants.SaleTableName);
        }
        
        public async Task<IEnumerable<Models.Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<Models.Sale>(x => x.PartitionKey == collectionId.ToString());
            return await saleQuery.GetAllAsync(ct);
        }

        public async Task UpdateOneAsync(Models.Sale sale, CancellationToken ct)
        {
            await _saleClient.UpsertEntityAsync(sale, TableUpdateMode.Merge, ct);
        }
    }
}
