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
        Task InsertOneAsync(NiftyCollection niftyCollection, CancellationToken ct);
    }

    public class NiftyCollectionRepository : INiftyCollectionRepository
    {
        private readonly TableClient _niftyCollectionClient;
        private readonly INiftyCollectionMapper _niftyCollectionMapper;

        public NiftyCollectionRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyCollectionMapper niftyCollectionMapper)
        {
            _niftyCollectionClient = tableClientFactory.CreateClient("NiftyCollection");
            _niftyCollectionMapper = niftyCollectionMapper;
        }

        public async Task<NiftyCollection?> GetById(Guid id, CancellationToken ct)
        {
            var niftyCollectionQuery = _niftyCollectionClient.QueryAsync<Models.NiftyCollection>(x => x.RowKey == id.ToString());
            var sales = await niftyCollectionQuery.GetAllAsync(ct); //TODO GetOneAsync
            return sales.Select(_niftyCollectionMapper.Map).FirstOrDefault();
        }

        public async Task InsertOneAsync(NiftyCollection niftyCollection, CancellationToken ct)
        {
            var niftyCollectionDto = _niftyCollectionMapper.Map(niftyCollection);
            await _niftyCollectionClient.AddEntityAsync(niftyCollectionDto, ct);
        }
    }
}
