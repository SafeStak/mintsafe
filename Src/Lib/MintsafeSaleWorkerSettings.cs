public record MintsafeSaleWorkerSettings(
    Network Network, 
    int PollingIntervalSeconds,
    string BasePath,
    string BlockFrostApiKey);

public enum Network { Mainnet, Testnet }