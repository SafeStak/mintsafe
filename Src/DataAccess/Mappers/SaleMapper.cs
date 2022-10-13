using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mappers
{
    public class SaleMapper
    {
        public static Models.Sale Map(Sale sale)
        {
            return new Models.Sale
            {
                RowKey = sale.Id.ToString(),
                PartitionKey = sale.CollectionId.ToString(),
                IsActive = sale.IsActive,
                Name = sale.Name,
                Description = sale.Description,
                LovelacesPerToken = sale.LovelacesPerToken,
                SaleAddress = sale.SaleAddress,
                CreatorAddress = sale.CreatorAddress,
                ProceedsAddress = sale.ProceedsAddress,
                PostPurchaseMargin = (double)sale.PostPurchaseMargin,
                TotalReleaseQuantity = sale.TotalReleaseQuantity,
                MaxAllowedPurchaseQuantity = sale.MaxAllowedPurchaseQuantity,
                Start = sale.Start.ToUniversalTime(),
                End = sale.End?.ToUniversalTime()
            };
        }

        public static Sale Map(Models.Sale saleDto)
        {
            if (saleDto == null) throw new ArgumentNullException(nameof(saleDto));
            if (saleDto.Name == null) throw new ArgumentNullException(nameof(saleDto.Name));
            if (saleDto.SaleAddress == null) throw new ArgumentNullException(nameof(saleDto.SaleAddress));
            if (saleDto.ProceedsAddress == null) throw new ArgumentNullException(nameof(saleDto.ProceedsAddress));
            if (saleDto.CreatorAddress == null) throw new ArgumentNullException(nameof(saleDto.CreatorAddress));

            return new Sale(
                Guid.Parse(saleDto.RowKey),
                Guid.Parse(saleDto.PartitionKey),
                saleDto.IsActive,
                saleDto.Name,
                saleDto.Description,
                saleDto.LovelacesPerToken,
                saleDto.SaleAddress,
                saleDto.CreatorAddress,
                saleDto.ProceedsAddress,
                (decimal)saleDto.PostPurchaseMargin,
                saleDto.TotalReleaseQuantity,
                saleDto.MaxAllowedPurchaseQuantity,
                saleDto.Start,
                saleDto.End
                );
        }
    }
}
