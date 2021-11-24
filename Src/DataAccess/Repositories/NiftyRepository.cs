using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyRepository
    {
        Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task InsertOneAsync(Nifty nifty, CancellationToken ct);
    }

    public class NiftyRepository : INiftyRepository
    {
        private readonly TableClient _niftyClient;
        private readonly INiftyMapper _niftyMapper;

        public NiftyRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyMapper niftyMapper)
        {
            _niftyClient = tableClientFactory.CreateClient("Nifty");
            _niftyMapper = niftyMapper;
        }

        public async Task<IEnumerable<Nifty>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<Models.Nifty>(x => x.PartitionKey == collectionId.ToString());
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales.Select(_niftyMapper.Map);
        }

        public async Task InsertOneAsync(Nifty nifty, CancellationToken ct)
        {
            var niftyCollectionDto = _niftyMapper.Map(nifty);
            await _niftyClient.AddEntityAsync(niftyCollectionDto, ct);
        }
    }
}