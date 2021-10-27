using System;
using System.Linq;

namespace NiftyLaunchpad.Lib
{
    public record NiftySalePeriod(
        Guid Id,
        Guid CollectionId,
        string PolicyId,
        string Name,
        string Description,
        long LovelacesPerToken,
        string SaleAddress,
        bool IsActive,
        DateTime From,
        DateTime To,
        int TotalReleaseQuantity,
        int MaxAllowedPurchaseQuantity);

    public record NiftySalePurchaseRequest(
        Guid SalePeriodId,
        string TxHash,
        int NiftyQuantityRequested,
        long ChangeInLovelace);

    public static class SalePurchaseRequester
    {
        public static NiftySalePurchaseRequest FromUtxo(Utxo utxo, NiftySalePeriod sale)
        {
            var lovelaceValue = utxo.Values.First(v => v.Unit == "lovelace");
            if (lovelaceValue.Quantity < sale.LovelacesPerToken)
                throw new InsufficientPaymentException($"Insufficient lovelaces for purchase", sale.LovelacesPerToken, lovelaceValue.Quantity, "lovelace");

            var quantity = (int)(lovelaceValue.Quantity / sale.LovelacesPerToken);
            if (quantity > sale.MaxAllowedPurchaseQuantity)
                throw new MaxAllowedPurchaseQuantityExceededException($"Max allowed purchase quantity exceeded", sale.MaxAllowedPurchaseQuantity, quantity);

            var change = lovelaceValue.Quantity % sale.LovelacesPerToken;

            return new NiftySalePurchaseRequest(
                SalePeriodId: sale.Id,
                TxHash: utxo.TxHash,
                NiftyQuantityRequested: quantity,
                ChangeInLovelace: change);
        }
    }

    public class InsufficientPaymentException : ApplicationException
    {
        public long QuantityPerToken { get; }
        public long ActualQuantity { get; }
        public string Unit { get; }

        public InsufficientPaymentException(
            string message, long quantityPerToken, long actualQuantity, string unit) : base(message) 
        {
            QuantityPerToken = quantityPerToken;
            ActualQuantity = actualQuantity;
            Unit = unit;
        }
    }

    public class MaxAllowedPurchaseQuantityExceededException : ApplicationException
    {
        public int MaxQuantity { get; }
        public int DerivedQuantity { get; }

        public MaxAllowedPurchaseQuantityExceededException(
            string message, int maxQuantity, int derivedQuantity) : base(message)
        {
            MaxQuantity = maxQuantity;
            DerivedQuantity = derivedQuantity;
        }
    }
}
