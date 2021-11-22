using Azure.Data.Tables;
using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mapping
{
    public interface ISaleMapper : ITableMapper<Sale> { }

    public class SaleMapper : ISaleMapper
    {
        public TableEntity ToTableEntity(Sale sale)
        {
            var tableEntity = new TableEntity
            {
                RowKey = sale.Id.ToString(),
                PartitionKey = sale.CollectionId.ToString(),
                ["IsActive"] = sale.IsActive,
                ["Name"] = sale.Name,
                ["Description"] = sale.Description,
                ["LovelacesPerToken"] = sale.LovelacesPerToken,
                ["SaleAddress"] = sale.SaleAddress,
                ["ProceedsAddress"] = sale.ProceedsAddress,
                ["TotalReleaseQuantity"] = sale.TotalReleaseQuantity,
                ["MaxAllowedPurchaseQuantity"] = sale.MaxAllowedPurchaseQuantity,
                ["Start"] = sale.Start, //TODO datetime tostring serialisation
                ["End"] = sale.End //TODO datetime tostring serialisation
            };

            return tableEntity;
        }

        public Sale FromTableEntity(TableEntity tableEntity)
        {
            //TODO generic type parsing
            return new Sale(
                Guid.Parse(tableEntity.RowKey),
                Guid.Parse(tableEntity.PartitionKey),
                (bool)tableEntity["IsActive"],
                (string)tableEntity["Name"],
                (string)tableEntity["Description"],
                (long)tableEntity["LovelacesPerToken"],
                (string)tableEntity["SaleAddress"],
                (string)tableEntity["ProceedsAddress"],
                (int)tableEntity["TotalReleaseQuantity"],
                (int)tableEntity["MaxAllowedPurchaseQuantity"],
                (DateTime?)tableEntity["Start"],
                (DateTime?)tableEntity["End"]
                );
        }
    }
}
