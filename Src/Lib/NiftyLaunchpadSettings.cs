public record NiftyLaunchpadSettings(
    Network Network, 
    int PollingIntervalSeconds,
    string BlockFrostApiKey);

public enum Network { Mainnet, Testnet }