using System.Linq;

namespace NiftyLaunchpad.Lib
{
    public record Utxo(string TxHash, int OutputIndex, UtxoValue[] Values);

    public record UtxoValue(string Unit, long Quantity);

    public static class UtxoExtensions
    {
        public static long Lovelaces(this Utxo utxo)
        {
            return utxo.Values.First(v => v.Unit == "lovelace").Quantity;
        }

        public static string ShortForm(this Utxo utxo)
        {
            return $"{utxo.TxHash}#{utxo.OutputIndex}";
        }
    }
}
