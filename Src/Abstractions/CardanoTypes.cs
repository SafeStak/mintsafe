using System.Linq;

public record TxBasic(string TxHash, TxIo[] Inputs, TxIo[] Outputs);
public record TxIo(string Address, int OutputIndex, UtxoValue[] Values);

public record Utxo(string TxHash, int OutputIndex, UtxoValue[] Values);
public record UtxoValue(string Unit, long Quantity);

public record TxBuildCommand(
    Utxo[] Inputs,
    TxOutput[] Outputs, 
    UtxoValue[] Mint, 
    string MintingScriptPath,
    string MetadataJsonPath,
    long TtlSlot);
public record TxOutput(string Address, UtxoValue[] Values);

public record TxCalculateFeeCommand(
    string TxRawPath,
    int TxInCount,
    int TxOutCount,
    int WitnessCount,
    string NetworkSegment,
    string ProtocolParamsPath);

public record TxSignCommand(
    string[] SigningKeyPaths,
    string TxRawPath,
    string NetworkSegment,
    string TxSignedOutputPath);

public record TxSubmitCommand(
    string TxSignedPath,
    string NetworkSegment);

public static class UtxoExtensions
{
    public static long Lovelaces(this Utxo utxo)
    {
        return utxo.Values.First(v => v.Unit == "lovelace").Quantity;
    }

    public static string ShortForm(this Utxo utxo)
    {
        return $"{utxo.TxHash}__{utxo.OutputIndex}";
    }
}