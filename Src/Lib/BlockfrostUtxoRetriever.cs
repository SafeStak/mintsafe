using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostUtxoRetriever : IUtxoRetriever
{
    private readonly ILogger<BlockfrostUtxoRetriever> _logger;
    private readonly IBlockfrostClient _blockFrostClient;
    
    public BlockfrostUtxoRetriever(
        ILogger<BlockfrostUtxoRetriever> logger,
        IBlockfrostClient blockFrostClient)
    {
        _logger = logger;
        _blockFrostClient = blockFrostClient;
    }

    public async Task<UnspentTransactionOutput[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
    {
        var bfResult = Array.Empty<BlockFrostAddressUtxo>();
        try
        {
            bfResult = await _blockFrostClient.GetUtxosAtAddressAsync(address, ct).ConfigureAwait(false);
            if (bfResult == null)
                throw new BlockfrostResponseException($"BlockFrost response for {nameof(GetUtxosAtAddressAsync)} is null", 200);
        }
        catch (Exception ex)
        {
            _logger.LogError(EventIds.UtxoRetrievalError, ex, "Unhandled exception from the BlockfrostClient");
            return Array.Empty<UnspentTransactionOutput>();
        }

        return bfResult.Select(MapBlockFrostUtxoToUtxo).ToArray();
    }

    private static UnspentTransactionOutput MapBlockFrostUtxoToUtxo(BlockFrostAddressUtxo bfUtxo)
    {
        if (bfUtxo.Tx_hash == null)
            throw new BlockfrostResponseException("Blockfrost response has null txhash", 0);
        if (bfUtxo.Amount == null) 
            throw new BlockfrostResponseException("Blockfrost response has null amount", 0);

        var lovelaces = 0UL;
        var index = 0;
        var nativeAssets = new NativeAssetValue[bfUtxo.Amount.Length - 1];
        foreach (var val in bfUtxo.Amount)
        {
            if (val.Unit == null)
                throw new BlockfrostResponseException("Blockfrost response has null unit", 0);
            if (val.Quantity == null)
                throw new BlockfrostResponseException("Blockfrost response has null quantity", 0);
            if (val.Unit == Assets.LovelaceUnit)
            {
                lovelaces = ulong.Parse(val.Quantity);
                continue;
            }
            var policyId = val.Unit[..56];
            var assetName = val.Unit[56..];
            nativeAssets[index++] = new NativeAssetValue(policyId, assetName, ulong.Parse(val.Quantity));
        }
        var aggValue = new AggregateValue(lovelaces, nativeAssets);

        return new UnspentTransactionOutput(
            bfUtxo.Tx_hash,
            (uint)bfUtxo.Output_index,
            aggValue);
    }
}
