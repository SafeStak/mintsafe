using Mintsafe.Abstractions;
using System;

namespace Mintsafe.Lib;

public record MintsafeAppSettings
{
    public Network Network { get; init; }
    public int PollingIntervalSeconds { get; init; }
    public string? BasePath { get; init; }
    public string? BlockFrostApiKey { get; init; }
    public string? AppInsightsInstrumentationKey { get; init; }
    public Guid CollectionId { get; init; }

    public Guid[] SaleIds { get; init; }
    public string? KeyVaultUrl { get; init; }
}
