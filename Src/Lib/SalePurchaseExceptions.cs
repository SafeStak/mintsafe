using System;

namespace NiftyLaunchpad.Lib
{
    public class SaleReleaseQuantityExceededException : ApplicationException
    {
        public long RequestedQuantity { get; }
        public Utxo PurchaseAttemptUtxo { get; }

        public SaleReleaseQuantityExceededException(
            string message, 
            Utxo purchaseAttemptUtxo,
            int requestedQuantity) : base(message)
        {
            RequestedQuantity = requestedQuantity;
            PurchaseAttemptUtxo = purchaseAttemptUtxo;
        }
    }

    public class NoMintableTokensLeftException : ApplicationException
    {
        public long RequestedQuantity { get; }
        public Utxo PurchaseAttemptUtxo { get; }

        public NoMintableTokensLeftException(
            string message, 
            Utxo purchaseAttemptUtxo,
            int requestedQuantity) : base(message)
        {
            RequestedQuantity = requestedQuantity;
            PurchaseAttemptUtxo = purchaseAttemptUtxo;
        }
    }

    public class InsufficientPaymentException : ApplicationException
    {
        public long QuantityPerToken { get; }
        public Utxo PurchaseAttemptUtxo { get; }

        public InsufficientPaymentException(
            string message, 
            Utxo purchaseAttemptUtxo, 
            long quantityPerToken) : base(message) 
        {
            QuantityPerToken = quantityPerToken;
            PurchaseAttemptUtxo = purchaseAttemptUtxo;
        }
    }

    public class MaxAllowedPurchaseQuantityExceededException : ApplicationException
    {
        public int MaxQuantity { get; }
        public int DerivedQuantity { get; }
        public Utxo PurchaseAttemptUtxo { get; }

        public MaxAllowedPurchaseQuantityExceededException(
            string message, 
            Utxo purchaseAttemptUtxo, 
            int maxQuantity, 
            int derivedQuantity) : base(message)
        {
            MaxQuantity = maxQuantity;
            DerivedQuantity = derivedQuantity;
            PurchaseAttemptUtxo = purchaseAttemptUtxo;
        }
    }

    public class SalePeriodOutOfRangeException : ApplicationException
    {
        public DateTime? SaleStartDateTime { get; }
        public DateTime? SaleEndDateTime { get; }
        public DateTime PurchaseAttemptedAt { get; }
        public Utxo PurchaseAttemptUtxo { get; }

        public SalePeriodOutOfRangeException(
            string message,
            Utxo purchaseAttemptUtxo,
            DateTime? saleStartDateTime, 
            DateTime? saleEndDateTime) : base(message)
        {
            SaleStartDateTime = saleStartDateTime;
            SaleEndDateTime = saleEndDateTime;
            PurchaseAttemptedAt = DateTime.UtcNow;
            PurchaseAttemptUtxo = purchaseAttemptUtxo;
        }
    }

    public class SaleInactiveException : ApplicationException
    {
        public Utxo PurchaseAttemptUtxo { get; }

        public SaleInactiveException(
            string message, Utxo purchaseAttemptUtxo) : base(message) 
        {
            PurchaseAttemptUtxo = purchaseAttemptUtxo;
        }
    }
}
