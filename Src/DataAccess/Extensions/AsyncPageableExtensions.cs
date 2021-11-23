using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Extensions
{
    internal static class AsyncPageableExtensions
    {
        //TODO is this the best method? batch reads?
        internal static async Task<IList<T>> GetAllAsync<T>(this AsyncPageable<T> tableQuery, CancellationToken ct) where T : ITableEntity
        {
            var entities = new List<T>();
            await foreach (Page<T> page in tableQuery.AsPages().WithCancellation(ct))
            {
                entities.AddRange(page.Values);
            }
            return entities;
        }
    }
}
