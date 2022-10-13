using Mintsafe.Abstractions;
using System;

namespace Mintsafe.Lib;

public class CannotAllocateMoreThanSaleReleaseException : ApplicationException
{
    public long RequestedQuantity { get; }
    public long SaleReleaseQuantity { get; }
    public long SaleAllocatedQuantity { get; }
    public Guid SaleId { get; }
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }

    public CannotAllocateMoreThanSaleReleaseException(
        string message,
        UnspentTransactionOutput purchaseAttemptUtxo,
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

public class InsufficientPaymentException : ApplicationException
{
    public ulong QuantityPerToken { get; }
    public Guid SaleId { get; }
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }

    public InsufficientPaymentException(
        string message,
        Guid saleId,
        UnspentTransactionOutput purchaseAttemptUtxo,
        ulong quantityPerToken) : base(message)
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
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }

    public PurchaseQuantityHardLimitException(
        string message,
        UnspentTransactionOutput purchaseAttemptUtxo,
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
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }

    public MaxAllowedPurchaseQuantityExceededException(
        string message,
        Guid saleId,
        UnspentTransactionOutput purchaseAttemptUtxo,
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
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }

    public SalePeriodOutOfRangeException(
        string message,
        Guid saleId,
        UnspentTransactionOutput purchaseAttemptUtxo,
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
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }
    public Guid SaleId { get; }

    public SaleInactiveException(
        string message,
        Guid saleId,
        UnspentTransactionOutput purchaseAttemptUtxo) : base(message)
    {
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class FailedUtxoRefundException : ApplicationException
{
    public UnspentTransactionOutput PurchaseAttemptUtxo { get; }
    public Guid SaleId { get; }

    public FailedUtxoRefundException(
        string message,
        Guid saleId,
        UnspentTransactionOutput purchaseAttemptUtxo,
        Exception? innerException) : base(message, innerException)
    {
        SaleId = saleId;
        PurchaseAttemptUtxo = purchaseAttemptUtxo;
    }
}

public class InputOutputValueMismatchException : ApplicationException
{
    public UnspentTransactionOutput[] Inputs { get; }
    public PendingTransactionOutput[] Outputs { get; }
    public string CorrelationId { get; }

    public InputOutputValueMismatchException(
        string message,
        UnspentTransactionOutput[] inputs,
        PendingTransactionOutput[] outputs,
        string? correlationId = null) : base(message)
    {
        Inputs = inputs;
        Outputs = outputs;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }
}