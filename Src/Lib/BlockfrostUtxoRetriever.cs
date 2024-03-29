﻿using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostUtxoRetriever : IUtxoRetriever
{
    private readonly ILogger<BlockfrostUtxoRetriever> _logger;
    private readonly BlockfrostClient _blockFrostClient;
    

    public BlockfrostUtxoRetriever(
        ILogger<BlockfrostUtxoRetriever> logger,
        BlockfrostClient blockFrostClient)
    {
        _logger = logger;
        _blockFrostClient = blockFrostClient;
    }

    public async Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
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
            return Array.Empty<Utxo>();
        }

        return bfResult.Select(MapBlockFrostUtxoToUtxo).ToArray();
    }

    private static Utxo MapBlockFrostUtxoToUtxo(BlockFrostAddressUtxo bfUtxo)
    {
        static Value MapValueFromAmount(BlockFrostValue bfVal)
        {
            if (bfVal.Quantity == null) 
                throw new BlockfrostResponseException("Blockfrost response has null amount.quantity", 0);

            return new Value(
                bfVal.Unit ?? throw new BlockfrostResponseException("Blockfrost response has null amount.unit", 0),
                long.Parse(bfVal.Quantity));
        }

        if (bfUtxo.Amount == null) 
            throw new BlockfrostResponseException("Blockfrost response has null amount", 0);

        return new Utxo(
            bfUtxo.Tx_hash ?? throw new BlockfrostResponseException("Blockfrost response has null tx_hash", 0),
            bfUtxo.Output_index,
            bfUtxo.Amount
                .Select(MapValueFromAmount).ToArray());
    }
}
