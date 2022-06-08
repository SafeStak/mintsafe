using System.Runtime.Serialization;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.Models
{
    public class Nifty : ITableEntity
    {
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }

        public bool IsMintable { get; set; }
        public string? AssetName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        [IgnoreDataMember]
        public string[]? Creators { get; set; }

        public string? CreatorsAsString
        {
            get => Creators != null ? string.Join(',', Creators.Where(x => !string.IsNullOrWhiteSpace(x))) : null;
            set => Creators = value?.Split(',');
        }

        public string? Image { get; set; }
        public string? MediaType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Version { get; set; }

        [IgnoreDataMember]
        public IEnumerable<KeyValuePair<string, string>>? Attributes { get; set; }

        public string AttributesAsString
        {
            get => JsonSerializer.Serialize(Attributes);
            set => Attributes = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<string, string>>>(value)!;
        }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
