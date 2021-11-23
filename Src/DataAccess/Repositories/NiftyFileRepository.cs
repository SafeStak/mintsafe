using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;

namespace Mintsafe.DataAccess.Repositories
{
    public class NiftyFileRepository
    {
        private readonly TableClient _niftyClient;
        private readonly INiftyFileMapper _niftyFileMapper;

        public NiftyFileRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyFileMapper niftyFileMapper)
        {
            _niftyClient = tableClientFactory.CreateClient("Nifty");
            _niftyFileMapper = niftyMapper;
        }

        public async Task<IEnumerable<NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyClient.QueryAsync<DTOs.NiftyFile>(x => x.PartitionKey == collectionId.ToString());
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales.Select(_niftyFileMapper.FromDto);
        }
    }
}
