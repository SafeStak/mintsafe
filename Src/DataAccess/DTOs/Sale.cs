using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.DTOs
{
    public class Sale : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public bool IsActive { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long LovelacesPerToken { get; set; }
        public string SaleAddress { get; set; }
        public string ProceedsAddress { get; set; }
        public int TotalReleaseQuantity { get; set; }
        public int MaxAllowedPurchaseQuantity { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
