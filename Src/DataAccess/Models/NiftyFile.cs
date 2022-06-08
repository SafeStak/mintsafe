using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Models
{
    public class NiftyFile : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public string? NiftyId { get; set; }
        public string? Name { get; set; }
        public string? MediaType { get; set; }
        public string? Url { get; set; }
        public string? FileHash { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
