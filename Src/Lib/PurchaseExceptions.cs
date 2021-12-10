using Mintsafe.Abstractions;
using System;

namespace Mintsafe.Lib;

public class CannotAllocateMoreThanSaleReleaseException : ApplicationException
{
    public long RequestedQuantity { get; }
    public long SaleReleaseQuantity { get; }
    public long SaleAllocatedQuantity { get; }
    public Guid SaleId { get; }
    public Utxo PurchaseAttemptUtxo { get; }

    public CannotAllocateMoreThanSaleReleaseException(
        string message,
        Utxo purchaseAttemptUtxo,
        Guid saleId,
        int saleReleaseQuantity,
        int saleAllocatedQuantity,
        int requestedQuantity) : base(message)
    {
        RequestedQuantity = requestedQuantity;
        SaleReleaseQuantity = saleReleaseQuantity;
        SaleAllocatedQuantity = saleAllocatedQuantity;
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class CannotAllocateMoreThanMintableException : ApplicationException
{
    public long RequestedQuantity { get; }
    public long MintableQuantity { get; }
    public Guid SaleId { get; }
    public Utxo PurchaseAttemptUtxo { get; }

    public CannotAllocateMoreThanMintableException(
        string message,
        Utxo purchaseAttemptUtxo,
        Guid saleId,
        int requestedQuantity,
        int mintableQuantity) : base(message)
    {
        RequestedQuantity = requestedQuantity;
        MintableQuantity = mintableQuantity;
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class InsufficientPaymentException : ApplicationException
{
    public long QuantityPerToken { get; }
    public Guid SaleId { get; }
    public Utxo PurchaseAttemptUtxo { get; }

    public InsufficientPaymentException(
        string message,
        Guid saleId,
        Utxo purchaseAttemptUtxo,
        long quantityPerToken) : base(message)
    {
        QuantityPerToken = quantityPerToken;
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class PurchaseQuantityHardLimitException : ApplicationException
{
    public long RequestedQuantity { get; }
    public Guid SaleId { get; }
    public Utxo PurchaseAttemptUtxo { get; }

    public PurchaseQuantityHardLimitException(
        string message,
        Utxo purchaseAttemptUtxo,
        Guid saleId,
        int requestedQuantity) : base(message)
    {
        RequestedQuantity = requestedQuantity;
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class MaxAllowedPurchaseQuantityExceededException : ApplicationException
{
    public int MaxQuantity { get; }
    public int DerivedQuantity { get; }
    public Guid SaleId { get; }
    public Utxo PurchaseAttemptUtxo { get; }

    public MaxAllowedPurchaseQuantityExceededException(
        string message,
        Guid saleId,
        Utxo purchaseAttemptUtxo,
        int maxQuantity,
        int derivedQuantity) : base(message)
    {
        MaxQuantity = maxQuantity;
        DerivedQuantity = derivedQuantity;
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class SalePeriodOutOfRangeException : ApplicationException
{
    public DateTime? SaleStartDateTime { get; }
    public DateTime? SaleEndDateTime { get; }
    public DateTime PurchaseAttemptedAt { get; }
    public Guid SaleId { get; }
    public Utxo PurchaseAttemptUtxo { get; }

    public SalePeriodOutOfRangeException(
        string message,
        Guid saleId,
        Utxo purchaseAttemptUtxo,
        DateTime? saleStartDateTime,
        DateTime? saleEndDateTime) : base(message)
    {
        SaleStartDateTime = saleStartDateTime;
        SaleEndDateTime = saleEndDateTime;
        PurchaseAttemptedAt = DateTime.UtcNow;
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class SaleInactiveException : ApplicationException
{
    public Utxo PurchaseAttemptUtxo { get; }
    public Guid SaleId { get; }

    public SaleInactiveException(
        string message,
        Guid saleId,
        Utxo purchaseAttemptUtxo) : base(message)
    {
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class FailedUtxoRefundException : ApplicationException
{
    public Utxo PurchaseAttemptUtxo { get; }
    public Guid SaleId { get; }

    public FailedUtxoRefundException(
        string message,
        Guid saleId,
        Utxo purchaseAttemptUtxo,
        Exception? innerException) : base(message, innerException)
    {
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}