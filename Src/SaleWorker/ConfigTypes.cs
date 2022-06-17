using System;

namespace Mintsafe.SaleWorker;

public class MintsafeWorkerConfig
{
    public string? MintBasePath { get; init; }
    public int? PollingIntervalSeconds { get; init; }
    public string? CollectionId { get; init; }
}

public class CardanoNetworkConfig
{
    public string? Network { get; init; }
    public int? Magic { get; init; }
}

public class BlockfrostApiConfig
{
    public string? BaseUrl { get; init; }
    public string? ApiKey { get; init; }
}

public class KeychainConfig
{
    public string? KeyVaultUrl { get; init; }
    public int? RetrievalMaxRetries { get; init; }
    public int? RetrievalRetryDelaySeconds { get; init; }
    public int? RetrievalRetryMaxDelaySeconds { get; init; }
}

public class ApplicationInsightsConfig
{
    public bool Enabled { get; init; }
    public string? InstrumentationKey { get; init; }
}

public class StorageConfig
{
    public string? ConnectionString { get; init; }
}

public class MintSafeConfigException : ApplicationException
{
    public string? Path { get; init; }

    public MintSafeConfigException(
        string message,
        string? path = null,
        Exception? innerException = null) : base(message, innerException)
    {
        Path = path;
    }
}