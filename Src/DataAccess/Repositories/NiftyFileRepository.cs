using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyFileRepository
    {
        Task<IEnumerable<NiftyFile>> GetByNiftyIds(IEnumerable<Guid> niftyIds, CancellationToken ct);
        Task InsertOneAsync(NiftyFile niftyFile, CancellationToken ct);
    }

    public class NiftyFileRepository : INiftyFileRepository
    {
        private readonly TableClient _niftyFileClient;
        private readonly INiftyFileMapper _niftyFileMapper;

        public NiftyFileRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyFileMapper niftyFileMapper)
        {
            _niftyFileClient = tableClientFactory.CreateClient("NiftyFile");
            _niftyFileMapper = niftyFileMapper;
        }

        public async Task<IEnumerable<NiftyFile>> GetByNiftyIds(IEnumerable<Guid> niftyIds, CancellationToken ct)
        {
            var niftyQuery = _niftyFileClient.QueryAsync<Models.NiftyFile>(x => x.PartitionKey == niftyIds.First().ToString()); //TODO fix query
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales.Select(_niftyFileMapper.Map);
        }

        public async Task InsertOneAsync(NiftyFile niftyFile, CancellationToken ct)
        {
            var niftyCollectionDto = _niftyFileMapper.Map(niftyFile);
            await _niftyFileClient.AddEntityAsync(niftyCollectionDto, ct);
        }
    }
}
