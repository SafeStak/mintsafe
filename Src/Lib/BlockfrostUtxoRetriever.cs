using Mintsafe.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostUtxoRetriever : IUtxoRetriever
{
    private readonly BlockfrostClient _blockFrostClient;

    public BlockfrostUtxoRetriever(BlockfrostClient blockFrostClient)
    {
        _blockFrostClient = blockFrostClient;
    }

    public async Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
    {
        var bfResult = await _blockFrostClient.GetUtxosAtAddressAsync(address, ct).ConfigureAwait(false);
        if (bfResult == null)
            throw new BlockfrostResponseException($"BlockFrost response for {nameof(GetUtxosAtAddressAsync)} is null", 200);

        return bfResult.Select(MapBlockFrostUtxoToUtxo).ToArray();
    }

    private static Utxo MapBlockFrostUtxoToUtxo(BlockFrostAddressUtxo b)
    {
        static Value MapValueFromAmount(BlockFrostValue ba)
        {
            if (ba.Quantity == null) throw new BlockfrostResponseException("Blockfrost response has null amount.quantity", 0);

            return new Value(
                ba.Unit ?? throw new BlockfrostResponseException("Blockfrost response has null amount.unit", 0),
                long.Parse(ba.Quantity));
        }

        if (b.Amount == null) throw new BlockfrostResponseException("Blockfrost response has null amount", 0);

        return new Utxo(
                b.Tx_hash ?? throw new BlockfrostResponseException("Blockfrost response has null tx_hash", 0),
                b.Output_index,
                b.Amount
                    .Select(MapValueFromAmount).ToArray());
    }
}
