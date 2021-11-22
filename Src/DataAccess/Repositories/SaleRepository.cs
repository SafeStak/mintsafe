using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;

namespace Mintsafe.DataAccess.Repositories
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task InsertOneAsync(Sale sale, CancellationToken ct);
        Task UpsertOneAsync(Sale sale, CancellationToken ct);
    }

    public class SaleRepository : ISaleRepository
    {
        private readonly TableClient _saleClient;
        private readonly ISaleMapper _saleMapper;

        public SaleRepository(IAzureClientFactory<TableClient> tableClientFactory, ISaleMapper saleMapper)
        {
            _saleClient = tableClientFactory.CreateClient("Sales");
            _saleMapper = saleMapper;
        }
        
        public async Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<TableEntity>(x => x.PartitionKey == collectionId.ToString()); //TODO define RowKey & PartitionKey
            var sales = await saleQuery.GetAllAsync(ct);
            return sales.Select(_saleMapper.FromTableEntity);
        }

        public async Task InsertOneAsync(Sale sale, CancellationToken ct)
        {
            //TODO set Id?
            var tableEntity = _saleMapper.ToTableEntity(sale);
            await _saleClient.AddEntityAsync(tableEntity, ct);
        }

        public async Task UpsertOneAsync(Sale sale, CancellationToken ct) //TODO Can update individual values and TableUpdateMode.Merge
        {
            var tableEntity = _saleMapper.ToTableEntity(sale);
            await _saleClient.UpsertEntityAsync(tableEntity, TableUpdateMode.Replace, ct);
        }
    }
}
