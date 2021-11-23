using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace Mintsafe.DataAccess.DTOs
{
    public class Nifty : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public bool IsMintable { get; set; }
        public string AssetName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [IgnoreDataMember]
        public string[] Creators { get; set; }

        public string CreatorsAsString
        {
            get => string.Join(',', Creators);
            set => Creators = value.Split(',');
        }

        public string Image { get; set; }
        public string MediaType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Version { get; set; }
        public double RoyaltyPortion { get; set; }
        public string RoyaltyAddress { get; set; }
        
        //TODO Attributes as JSON

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
