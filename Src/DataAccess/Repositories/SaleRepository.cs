using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Sale>> GetBySaleIdAsync(Guid saleId, CancellationToken ct);
        Task<IEnumerable<Sale>> GetByCollectionIdAsync(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Sale sale, CancellationToken ct);
        Task InsertOneAsync(Sale sale, CancellationToken ct);
        Task InsertManyAsync(IEnumerable<Sale> sales, CancellationToken ct);
    }

    public class SaleRepository : ISaleRepository
    {
        private readonly TableClient _saleClient;

        public SaleRepository(IAzureClientFactory<TableClient> tableClientFactory)
        {
            _saleClient = tableClientFactory.CreateClient(Constants.TableNames.Sale);
        }

        public async Task<IEnumerable<Sale>> GetBySaleIdAsync(Guid saleId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<Models.Sale>(x => x.RowKey == saleId.ToString());
            return await saleQuery.GetAllAsync(ct);
        }

        public async Task<IEnumerable<Sale>> GetByCollectionIdAsync(Guid collectionId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<Models.Sale>(x => x.PartitionKey == collectionId.ToString());
            return await saleQuery.GetAllAsync(ct);
        }

        public async Task UpdateOneAsync(Sale sale, CancellationToken ct)
        {
            await _saleClient.UpdateEntityAsync(sale, sale.ETag, TableUpdateMode.Merge, ct);
        }

        public async Task InsertOneAsync(Sale sale, CancellationToken ct)
        {
            await _saleClient.AddEntityAsync(sale, ct);
        }

        public async Task InsertManyAsync(IEnumerable<Sale> sales, CancellationToken ct)
        {
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(sales.Select(nfc => new TableTransactionAction(TableTransactionActionType.Add, nfc)));
            await _saleClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}
