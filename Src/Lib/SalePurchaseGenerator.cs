using System;
using System.Linq;

namespace NiftyLaunchpad.Lib
{
    public static class SalePurchaseGenerator
    {
        public static NiftySalePurchaseRequest FromUtxo(Utxo utxo, NiftySale sale)
        {
            if (!sale.IsActive)
                throw new SaleInactiveException("Sale is inactive", utxo);

            if (sale.Start.HasValue && sale.Start > DateTime.UtcNow)
                throw new SalePeriodOutOfRangeException("Sale has not started", utxo, sale.Start, sale.End);

            if (sale.End.HasValue && sale.End < DateTime.UtcNow)
                throw new SalePeriodOutOfRangeException("Sale has already ended", utxo, sale.Start, sale.End);

            var lovelaceValue = utxo.Values.First(v => v.Unit == "lovelace");
            if (lovelaceValue.Quantity < sale.LovelacesPerToken)
                throw new InsufficientPaymentException($"Insufficient lovelaces for purchase", utxo, sale.LovelacesPerToken);

            var quantity = (int)(lovelaceValue.Quantity / sale.LovelacesPerToken);
            if (quantity > sale.MaxAllowedPurchaseQuantity)
                throw new MaxAllowedPurchaseQuantityExceededException($"Max allowed purchase quantity exceeded", utxo, sale.MaxAllowedPurchaseQuantity, quantity);

            var change = lovelaceValue.Quantity % sale.LovelacesPerToken;

            return new NiftySalePurchaseRequest(
                Id: Guid.NewGuid(),
                SaleId: sale.Id,
                Utxo: utxo,
                NiftyQuantityRequested: quantity,
                ChangeInLovelace: change);
        }
    }
}
