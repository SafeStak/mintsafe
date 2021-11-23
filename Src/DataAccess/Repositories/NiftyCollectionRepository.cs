using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyCollectionRepository
    {
        Task<NiftyCollection?> GetById(Guid collectionId, CancellationToken ct);
    }

    public class NiftyCollectionRepository : INiftyCollectionRepository
    {
        private readonly TableClient _niftyClient;
        private readonly INiftyCollectionMapper _niftyCollectionMapper;

        public NiftyCollectionRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyCollectionMapper niftyCollectionMapper)
        {
            _niftyClient = tableClientFactory.CreateClient("NiftyCollection");
            _niftyCollectionMapper = niftyCollectionMapper;
        }

        public async Task<NiftyCollection?> GetById(Guid id, CancellationToken ct)
        {
            var niftyCollectionQuery = _niftyClient.QueryAsync<DTOs.NiftyCollection>(x => x.RowKey == id.ToString());
            var sales = await niftyCollectionQuery.GetAllAsync(ct); //TODO GetOneAsync
            return sales.Select(_niftyCollectionMapper.FromDto).FirstOrDefault();
        }
    }
}
