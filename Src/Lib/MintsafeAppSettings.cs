using System;

namespace Mintsafe.Lib;

public record MintsafeAppSettings(
    Network Network, 
    int PollingIntervalSeconds,
    string BasePath,
    string BlockFrostApiKey,
    Guid CollectionId);

public enum Network { Mainnet, Testnet }