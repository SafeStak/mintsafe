namespace Mintsafe.SaleWorker;
public static class LogEventIds
{
    public const int UnhandledError = 10001;
    public const int SaleInactive = 20001;
    public const int SalePeriodOutOfRange = 20002;
    public const int InsufficientPayment = 20003;
    public const int MaxAllowedPurchaseQuantityExceeded = 20004;
    public const int CannotAllocateMoreThanSaleRelease = 20005;
    public const int CannotAllocateMoreThanMintable = 20006;
    public const int BlockfrostServerErrorResponse = 30001;
    public const int BlockfrostBadRequestResponse = 30002;
    public const int BlockfrostTooManyRequestsResponse = 30003;
    public const int BlockfrostTimeout = 30004;
    public const int CardanoCliUnhandledError = 40001;
}
