using System;
using System.Collections.Generic;
using System.Linq;

namespace Mintsafe.Abstractions;

public enum Network { Mainnet, Testnet }

public static class Assets
{
    public const string LovelaceUnit = "lovelace";
}

public record struct Value(string Unit, ulong Quantity);

public record Utxo(string TxHash, int OutputIndex, Value[] Values)
{
    public override int GetHashCode() => ToString().GetHashCode();
    public override string ToString() => $"{TxHash}_{OutputIndex}";
    bool IEquatable<Utxo>.Equals(Utxo? other) => other != null && TxHash == other.TxHash && OutputIndex == other.OutputIndex;
    public ulong Lovelaces => Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;
}

// TODO: Ideal types
public record struct NativeAssetValue(string PolicyId, string AssetName, ulong Quantity);

public record struct Balance(ulong Lovelaces, NativeAssetValue[] NativeAssets);

public record struct PendingTransactionOutput(string Address, Balance Value);

public record UnspentTransactionOutput(string TxHash, uint OutputIndex, Balance Value)
{
    public override int GetHashCode() => ToString().GetHashCode();
    public override string ToString() => $"{TxHash}_{OutputIndex}";
    bool IEquatable<UnspentTransactionOutput>.Equals(UnspentTransactionOutput? other)
        => other != null && TxHash == other.TxHash && OutputIndex == other.OutputIndex;
    public ulong Lovelaces => Value.Lovelaces;
}
public record TransactionSummary(string TxHash, TransactionIo[] Inputs, TransactionIo[] Outputs);
public record TransactionIo(string Address, int OutputIndex, Balance Values);

public record BasicMintingPolicy(string[] PolicySigningKeysAll, uint ExpirySlot);
public record Mint(BasicMintingPolicy BasicMintingPolicy, NativeAssetValue[] NativeAssetsToMint);

public record ProtocolParams(uint MajorVersion, uint MinorVersion, uint MinFeeA, uint MinFeeB, uint CoinsPerUtxoWord);
public record NetworkContext(uint LatestSlot, ProtocolParams ProtocolParams);

public record BuildTransactionCommand(
    UnspentTransactionOutput[] Inputs,
    PendingTransactionOutput[] Outputs,
    Mint[] Mint,
    Dictionary<int, Dictionary<string, object>> Metadata,
    string[] PaymentSigningKeys,
    Network Network,
    uint TtlTipOffsetSlots = 5400);

public record RewardsWithdrawal(string StakeAddress, uint RewardLovelaces);

public record BuildTxCommand(
    UnspentTransactionOutput[] Inputs,
    PendingTransactionOutput[] Outputs,
    Network Network,
    NativeAssetValue[]? Mint = null,
    Dictionary<int, Dictionary<string, object>>? Metadata = null,
    RewardsWithdrawal[]? RewardsWithdrawals = null,
    SimpleScript[]? SimpleScripts = null,
    string[]? SigningKeys = null,
    uint TtlTipOffsetSlots = 7200);

public record BuiltTransaction(string TxHash, byte[] CborBytes);

public record TxIo(string Address, uint OutputIndex, Value[] Values);

public record TxInfo(string TxHash, TxIo[] Inputs, TxIo[] Outputs);

public record TxBuildOutput(string Address, Balance Values, bool IsFeeDeducted = false);

public enum NativeScriptType { PubKeyHash = 0, All, Any, AtLeast, InvalidBefore, InvalidAfter }
public record SimpleScript(
    NativeScriptType Type, 
    uint? AtLeast = null, 
    SimpleScript[]? Scripts = null,
    string? PubKeyHash = null, 
    uint? InvalidBefore = null, 
    uint? InvalidAfter = null);

//public interface INativeScript { }
//public class ScriptPubKey : INativeScript
//{
//    public byte[] KeyHash { get; init; }
//}
//public class ScriptInvalidAfter : INativeScript
//{
//    public ulong After { get; init; }
//}
//public class ScriptInvalidBefore : INativeScript
//{
//    public ulong Before { get; init; }
//}
//public class ScriptAll : INativeScript
//{
//    public INativeScript[] Scripts { get; init; }
//}
//public class ScriptAny : INativeScript
//{
//    public INativeScript[] Scripts { get; init; }
//}
//public class ScriptNofK : INativeScript
//{
//    public int N { get; init; }
//    public INativeScript[] Scripts { get; init; }
//}