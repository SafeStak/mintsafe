using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Extensions
{
    internal static class AsyncPageableExtensions
    {
        internal static async Task<IList<T>> GetAllAsync<T>(this AsyncPageable<T> tableQuery, CancellationToken ct) where T : ITableEntity
        {
            var entities = new List<T>();
            await foreach (Page<T> page in tableQuery.AsPages().WithCancellation(ct))
            {
                entities.AddRange(page.Values);
            }
            return entities;
        }

        internal static async Task<T?> GetFirstAsync<T>(this AsyncPageable<T> tableQuery, CancellationToken ct) where T : ITableEntity
        {
            await foreach (Page<T> page in tableQuery.AsPages().WithCancellation(ct))
            {
                return page.Values.FirstOrDefault();
            }

            return default;
        }
    }
}
