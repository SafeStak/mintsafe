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
            _saleClient = tableClientFactory.CreateClient("Sale");
            _saleMapper = saleMapper;
        }
        
        public async Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<DTOs.Sale>(x => x.PartitionKey == collectionId.ToString()); //TODO unit test queries
            var sales = await saleQuery.GetAllAsync(ct);
            return sales.Select(_saleMapper.FromDto);
        }

        public async Task InsertOneAsync(Sale sale, CancellationToken ct)
        {
            //TODO set Id?
            var saleDto = _saleMapper.ToDto(sale);
            await _saleClient.AddEntityAsync(saleDto, ct);
        }

        public async Task UpsertOneAsync(Sale sale, CancellationToken ct) //TODO Can update individual values and TableUpdateMode.Merge
        {
            var saleDto = _saleMapper.ToDto(sale);
            await _saleClient.UpsertEntityAsync(saleDto, TableUpdateMode.Merge, ct);
        }
    }
}
