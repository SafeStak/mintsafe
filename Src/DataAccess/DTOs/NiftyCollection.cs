using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.DTOs
{
    public class NiftyCollection : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public string PolicyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string BrandImage { get; set; }

        [IgnoreDataMember]
        public string[] Publishers { get; set; }

        public string PublishersAsString
        {
            get => string.Join(',', Publishers);
            set => Publishers = value.Split(',');
        }

        public DateTime CreatedAt { get; set; }
        public DateTime LockedAt { get; set; }
        public long SlotExpiry { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
