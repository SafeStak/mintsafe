namespace Mintsafe.Lib
{
    public static class EventIds
    {
        public const int GeneralError = 100000;
        public const int GeneralDebug = 100001;
        public const int GeneralInfo = 100002;
        public const int GeneralWarning = 100003;
        public const int GeneralInstrumentorMeasuredElapsed = 100004;
        public const int HostedServiceError = 110000;
        public const int HostedServiceDebug = 110001;
        public const int HostedServiceInfo = 110002;
        public const int HostedServiceWarning = 110003;
        public const int HostedServiceStarted = 110011;
        public const int HostedServiceFinished = 110012;
        public const int DataServiceRetrievalError = 120000;
        public const int DataServiceRetrievalWarning = 120003;
        public const int DataServiceRetrievalElapsed = 120004;
        public const int SaleContextWriteElapsed = 120005;
        public const int SaleContextGetOrRestoreElapsed = 120006;
        public const int SaleContextAllocateElapsed = 120016;
        public const int SaleContextReleaseElapsed = 120016;
        public const int UtxoRetrievalError = 130000;
        public const int UtxoRetrievalElapsed = 130004;
        public const int SaleHandlerUnhandledError = 140000;
        public const int SaleHandlerElapsed = 140004;
        public const int SaleInactive = 140011;
        public const int SalePeriodOutOfRange = 140012;
        public const int InsufficientPayment = 140013;
        public const int MaxAllowedPurchaseQuantityExceeded = 140014;
        public const int CannotAllocateMoreThanSaleRelease = 140015;
        public const int PurchaseQuantityHardLimitExceeded = 140016;
        public const int MetadataFileElapsed = 150004;
        public const int TxInfoRetrievalError = 160000;
        public const int TxInfoRetrievalElapsed = 160004;
        public const int TxBuilderError = 170000;
        public const int TxBuilderElapsed = 170004;
        public const int TxSubmissionError = 180000;
        public const int TxSubmissionElapsed = 180004;
        public const int DistributorElapsed = 190004;
        public const int UtxoRefunderError = 200000;
        public const int UtxoRefunderElapsed = 200004;
        public const int PaymentElapsed = 210004;
        public const int BlockfrostServerErrorResponse = 30001;
        public const int BlockfrostBadRequestResponse = 30002;
        public const int BlockfrostTooManyRequestsResponse = 30003;
        public const int BlockfrostTimeout = 30004;
    }
}
