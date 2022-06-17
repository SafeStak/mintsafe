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

public record struct AggregateValue(ulong Lovelaces, NativeAssetValue[] NativeAssets);

public record struct PendingTransactionOutput(string Address, AggregateValue Value);

public record UnspentTransactionOutput(string TxHash, uint OutputIndex, AggregateValue Value)
{
    public override int GetHashCode() => ToString().GetHashCode();
    public override string ToString() => $"{TxHash}_{OutputIndex}";
    bool IEquatable<UnspentTransactionOutput>.Equals(UnspentTransactionOutput? other)
        => other != null && TxHash == other.TxHash && OutputIndex == other.OutputIndex;
    public ulong Lovelaces => Value.Lovelaces;
}
public record TransactionSummary(string TxHash, TransactionIo[] Inputs, TransactionIo[] Outputs);
public record TransactionIo(string Address, int OutputIndex, AggregateValue Values);

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
public record BuiltTransaction(string TxHash, byte[] Bytes);
// End TODO

public record TxIo(string Address, uint OutputIndex, Value[] Values);

public record TxInfo(string TxHash, TxIo[] Inputs, TxIo[] Outputs);

public record TxBuildOutput(string Address, AggregateValue Values, bool IsFeeDeducted = false);

public record TxBuildCommand(
    UnspentTransactionOutput[] Inputs,
    TxBuildOutput[] Outputs,
    NativeAssetValue[] Mint,
    string MintingScriptPath,
    string MetadataJsonPath,
    long TtlSlot,
    string[] SigningKeyFiles);

public interface INativeScript { }
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