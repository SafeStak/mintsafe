using System;

namespace Mintsafe.Lib;

public enum Network { Mainnet, Testnet }

public record MintsafeAppSettings
{
    public Network Network { get; init; }
    public int PollingIntervalSeconds { get; init; }
    public string? BasePath { get; init; }
    public string? BlockFrostApiKey { get; init; }
    public string? AppInsightsInstrumentationKey { get; init; }
    public Guid CollectionId { get; init; }
}
