using Azure.Data.Tables;
using Microsoft.Extensions.Azure;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Mapping;
using Mintsafe.DataAccess.Supporting;

namespace Mintsafe.DataAccess.Repositories
{
    public interface INiftyFileRepository
    {
        Task<IEnumerable<NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct);
        Task UpdateOneAsync(Guid collectionId, NiftyFile niftyFile, CancellationToken ct);
        Task UpdateManyAsync(Guid collectionId, IEnumerable<NiftyFile> niftyFiles, CancellationToken ct);
    }

    public class NiftyFileRepository : INiftyFileRepository
    {
        private readonly TableClient _niftyFileClient;
        private readonly INiftyFileMapper _niftyFileMapper;

        public NiftyFileRepository(IAzureClientFactory<TableClient> tableClientFactory, INiftyFileMapper niftyFileMapper)
        {
            _niftyFileClient = tableClientFactory.CreateClient(Constants.NiftyFileTableName);
            _niftyFileMapper = niftyFileMapper;
        }

        public async Task<IEnumerable<NiftyFile>> GetByCollectionId(Guid collectionId, CancellationToken ct)
        {
            var niftyQuery = _niftyFileClient.QueryAsync<Models.NiftyFile>(x => x.PartitionKey == collectionId.ToString());
            var sales = await niftyQuery.GetAllAsync(ct);

            return sales.Select(_niftyFileMapper.Map);
        }

        public async Task UpdateOneAsync(Guid collectionId, NiftyFile niftyFile, CancellationToken ct)
        {
            //TODO assign new guid as id
            var niftyCollectionDto = _niftyFileMapper.Map(collectionId, niftyFile);
            await _niftyFileClient.UpsertEntityAsync(niftyCollectionDto, TableUpdateMode.Merge, ct);
        }

        public async Task UpdateManyAsync(Guid collectionId, IEnumerable<NiftyFile> niftyFiles, CancellationToken ct)
        {
            //TODO assign new guid as id
            var niftyCollectionDtos = niftyFiles.Select(nf => _niftyFileMapper.Map(collectionId, nf));
            List<TableTransactionAction> addEntitiesBatch = new List<TableTransactionAction>();
            addEntitiesBatch.AddRange(niftyCollectionDtos.Select(nf => new TableTransactionAction(TableTransactionActionType.Add, nf)));
            await _niftyFileClient.SubmitTransactionAsync(addEntitiesBatch, ct).ConfigureAwait(false);
        }
    }
}
