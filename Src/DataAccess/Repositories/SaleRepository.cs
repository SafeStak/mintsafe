using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface ISaleRepository
    {
        Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Sale sale, CancellationToken ct);
    }

    public class SaleRepository : ISaleRepository
    {
        private readonly TableClient _saleClient;
        private readonly ISaleMapper _saleMapper;

        public SaleRepository(IAzureClientFactory<TableClient> tableClientFactory, ISaleMapper saleMapper)
        {
            _saleClient = tableClientFactory.CreateClient(Constants.SaleTableName);
            _saleMapper = saleMapper;
        }
        
        public async Task<IEnumerable<Sale>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var saleQuery = _saleClient.QueryAsync<Models.Sale>(x => x.PartitionKey == collectionId.ToString());
            var sales = await saleQuery.GetAllAsync(ct);
            return sales.Select(_saleMapper.Map);
        }

        public async Task UpdateOneAsync(Sale sale, CancellationToken ct)
        {
            //TODO assign new guid as id
            var saleDto = _saleMapper.Map(sale);
            await _saleClient.UpsertEntityAsync(saleDto, TableUpdateMode.Merge, ct);
        }
    }
}
