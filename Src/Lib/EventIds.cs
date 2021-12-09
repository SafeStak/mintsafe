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
        public const int UtxoRetrievalElapsed = 130004;
        public const int SaleHandlerElapsed = 140004;
        public const int AllocatorElapsed = 150004;
        public const int MetadataFileElapsed = 160004;
        public const int TxInfoRetrievalElapsed = 170004;
        public const int TxBuilderElapsed = 180004;
        public const int TxSubmissionElapsed = 190004;
        public const int DistributorElapsed = 200004;
        public const int UtxoRefunderElapsed = 210004;
        public const int PaymentElapsed = 220004;
    }
}
