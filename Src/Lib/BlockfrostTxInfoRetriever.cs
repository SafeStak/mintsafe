using Mintsafe.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostTxInfoRetriever : ITxInfoRetriever
{
    private readonly BlockfrostClient _blockFrostClient;

    public BlockfrostTxInfoRetriever(BlockfrostClient blockFrostClient)
    {
        _blockFrostClient = blockFrostClient;
    }

    public async Task<TxInfo> GetTxInfoAsync(string txHash, CancellationToken ct = default)
    {
        var bfResult = await _blockFrostClient.GetTransactionAsync(txHash, ct).ConfigureAwait(false);
        // Null checks in case Blockfrost gives dodgy responses
        if (bfResult.Hash == null || bfResult.Inputs == null || bfResult.Outputs == null)
            throw new BlockfrostResponseException("BlockFrost response contains null fields", 200);

        return new TxInfo(
                bfResult.Hash,
                bfResult.Inputs.Select(r => new TxIo(r.Address, r.Output_Index, Array.Empty<Value>())).ToArray(),
                bfResult.Outputs.Select(r => new TxIo(r.Address, r.Output_Index, Array.Empty<Value>())).ToArray());
    }
}

public class FakeTxIoRetriever : ITxInfoRetriever
{
    public async Task<TxInfo> GetTxInfoAsync(string txHash, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);

        return new TxInfo(
            txHash,
            Inputs: new[] { new TxIo("addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x", 0, new[] { new Value(Assets.LovelaceUnit, 10200000) }) },
            Outputs: new[] { new TxIo("addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x", 0, new[] { new Value(Assets.LovelaceUnit, 10000000) }) });
    }
}
