using System;

public record MintsafeSaleWorkerSettings(
    Network Network, 
    int PollingIntervalSeconds,
    string BasePath,
    string BlockFrostApiKey,
    Guid CollectionId);

public enum Network { Mainnet, Testnet }