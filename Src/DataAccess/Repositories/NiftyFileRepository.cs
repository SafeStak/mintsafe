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
        Task UpsertOneAsync(NiftyFile niftyFile, CancellationToken ct);
        Task UpsertManyAsync(IEnumerable<NiftyFile> niftyFiles, CancellationToken ct);
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

        public async Task UpsertOneAsync(NiftyFile niftyFile, CancellationToken ct)
        {
            //TODO assign new guid as id
            var niftyCollectionDto = _niftyFileMapper.Map(niftyFile);
            await _niftyFileClient.UpsertEntityAsync(niftyCollectionDto, TableUpdateMode.Merge, ct);
        }

        public async Task UpsertManyAsync(IEnumerable<NiftyFile> niftyFiles, CancellationToken ct)
        {
            //TODO assign new guid as id
            var niftyCollectionDtos = niftyFiles.Select(_niftyFileMapper.Map);
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(niftyCollectionDtos.Select(nf => new TableTransactionAction(TableTransactionActionType.Add, nf)));
            await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}
