using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Repositories
{
    public abstract class RepositoryBase
    {
        //TODO to extension method
        internal async Task<IList<TableEntity>> GetAllAsync(AsyncPageable<TableEntity> tableQuery, CancellationToken ct)
        {
            var entities = new List<TableEntity>();
            await foreach (Page<TableEntity> page in tableQuery.AsPages().WithCancellation(ct))
            {
                entities.AddRange(page.Values);
            }
            return entities;
        }
    }
}
