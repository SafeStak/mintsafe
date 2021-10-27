using System;
using System.Linq;

namespace NiftyLaunchpad.Lib
{
    public record NiftySalePeriod(
        Guid Id,
        Guid CollectionId,
        string PolicyId,
        bool IsActive,
        string Name,
        string Description,
        long LovelacesPerToken,
        string SaleAddress,
        int TotalReleaseQuantity,
        int MaxAllowedPurchaseQuantity,
        DateTime? Start = null,
        DateTime? End = null);

    public record NiftySalePurchaseRequest(
        Guid SalePeriodId,
        string TxHash,
        int NiftyQuantityRequested,
        long ChangeInLovelace);

    public static class SalePurchaseRequester
    {
        public static NiftySalePurchaseRequest FromUtxo(Utxo utxo, NiftySalePeriod sale)
        {
            if (!sale.IsActive)
                throw new SaleInactiveException("Sale is inactive");

            if (sale.Start.HasValue && sale.Start > DateTime.UtcNow)
                throw new SalePeriodOutOfRangeException("Sale has not started", sale.Start, sale.End);

            if (sale.End.HasValue && sale.End < DateTime.UtcNow)
                throw new SalePeriodOutOfRangeException("Sale has ended", sale.Start, sale.End);

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

    public class SalePeriodOutOfRangeException : ApplicationException
    {
        public DateTime? SaleStartDateTime { get; }
        public DateTime? SaleEndDateTime { get; }
        public DateTime Now { get; }

        public SalePeriodOutOfRangeException(
            string message, DateTime? saleStartDateTime, DateTime? saleEndDateTime) : base(message)
        {
            SaleStartDateTime = saleStartDateTime;
            SaleEndDateTime = saleEndDateTime;
            Now = DateTime.UtcNow;
        }
    }

    public class SaleInactiveException : ApplicationException
    {
        public SaleInactiveException(
            string message) : base(message) { }
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
