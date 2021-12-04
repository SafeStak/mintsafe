﻿using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mappers
{
    public class SaleMapper
    {
        public static Models.Sale Map(Sale sale)
        {
            return new Models.Sale //TODO System properties e.g eTag
            {
                RowKey = sale.Id.ToString(),
                PartitionKey = sale.CollectionId.ToString(),
                IsActive = sale.IsActive,
                Name = sale.Name,
                Description = sale.Description,
                LovelacesPerToken = sale.LovelacesPerToken,
                SaleAddress = sale.SaleAddress,
                ProceedsAddress = sale.ProceedsAddress,
                TotalReleaseQuantity = sale.TotalReleaseQuantity,
                MaxAllowedPurchaseQuantity = sale.MaxAllowedPurchaseQuantity,
                Start = sale.Start,
                End = sale.End
            };
        }

        public static Sale Map(Models.Sale saleDto)
        {
            return new Sale(
                Guid.Parse(saleDto.RowKey),
                Guid.Parse(saleDto.PartitionKey),
                saleDto.IsActive,
                saleDto.Name,
                saleDto.Description,
                saleDto.LovelacesPerToken,
                saleDto.SaleAddress,
                saleDto.ProceedsAddress,
                saleDto.TotalReleaseQuantity,
                saleDto.MaxAllowedPurchaseQuantity,
                saleDto.Start,
                saleDto.End
                );
        }
    }
}