using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Extensions
{
    internal static class AsyncPageableExtensions
    {
        //TODO is this the best method?
        internal static async Task<IList<TableEntity>> GetAllAsync(this AsyncPageable<TableEntity> tableQuery, CancellationToken ct)
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
