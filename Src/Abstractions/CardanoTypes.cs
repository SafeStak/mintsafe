using System;
using System.Linq;

namespace Mintsafe.Abstractions;

public static class Assets
{
    public const string LovelaceUnit = "lovelace";
}

public record struct Value(string Unit, long Quantity);

public record Utxo(string TxHash, int OutputIndex, Value[] Values)
{
    public override int GetHashCode() => ToString().GetHashCode();
    public override string ToString() => $"{TxHash}_{OutputIndex}";
    bool IEquatable<Utxo>.Equals(Utxo? other) => other != null && TxHash == other.TxHash && OutputIndex == other.OutputIndex;
    public long Lovelaces => Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;
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
// End TODO

public record TxIo(string Address, int OutputIndex, Value[] Values);

public record TxInfo(string TxHash, TxIo[] Inputs, TxIo[] Outputs);

public record TxBuildOutput(string Address, Value[] Values, bool IsFeeDeducted = false);

public record TxBuildCommand(
    Utxo[] Inputs,
    TxBuildOutput[] Outputs,
    Value[] Mint,
    string MintingScriptPath,
    string MetadataJsonPath,
    long TtlSlot,
    string[] SigningKeyFiles);
