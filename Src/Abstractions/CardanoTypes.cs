using System.Linq;

namespace Mintsafe.Abstractions
{
    public record TxIoAggregate(string TxHash, TxIo[] Inputs, TxIo[] Outputs);
    public record TxIo(string Address, int OutputIndex, Value[] Values);

    public record Utxo(string TxHash, int OutputIndex, Value[] Values)
    {
        public override int GetHashCode() => ToString().GetHashCode();
        public override string ToString() => $"{TxHash}__{OutputIndex}";
        public long Lovelaces => Values.First(v => v.Unit == "lovelace").Quantity;
    }

    public record Value(string Unit, long Quantity);

    public record TxBuildCommand(
        Utxo[] Inputs,
        TxOutput[] Outputs,
        Value[] Mint,
        string MintingScriptPath,
        string MetadataJsonPath,
        long TtlSlot,
        string[] SigningKeyFiles);
    public record TxOutput(string Address, Value[] Values, bool IsFeeDeducted = false);
}