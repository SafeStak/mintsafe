namespace NiftyLaunchpad.Lib
{
    public record Utxo(string TxHash, int OutputIndex, UtxoValue[] Values);
    public record UtxoValue(string Unit, long Quantity);
}
