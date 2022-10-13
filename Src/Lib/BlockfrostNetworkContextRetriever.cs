using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostNetworkContextRetriever : INetworkContextRetriever
{
    private readonly IBlockfrostClient _blockFrostClient;
    private readonly IInstrumentor _instrumentor;

    public BlockfrostNetworkContextRetriever(IBlockfrostClient blockFrostClient, IInstrumentor instrumentor)
    {
        _blockFrostClient = blockFrostClient;
        _instrumentor = instrumentor;
    }

    public async Task<NetworkContext> GetNetworkContext(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var tipTask = _blockFrostClient.GetLatestBlockAsync(ct).ConfigureAwait(false);
            var protocolParamsTask = _blockFrostClient.GetLatestProtocolParameters(ct).ConfigureAwait(false);
            var tip = await tipTask;
            var protocolParams = await protocolParamsTask;

            // Null checks in case Blockfrost gives dodgy responses
            if (tip.Slot == null)
                throw new BlockfrostResponseException("BlockFrost response contains null fields", 200);
            if (protocolParams.Protocol_major_ver == null || protocolParams.Protocol_minor_ver == null
                || protocolParams.Min_fee_a == null || protocolParams.Min_fee_b == null || protocolParams.Coins_per_utxo_word == null)
                throw new BlockfrostResponseException("BlockFrost response contains null fields", 200);

            return new NetworkContext(
                LatestSlot: tip.Slot.Value,
                new ProtocolParams(
                    MajorVersion: protocolParams.Protocol_major_ver.Value,
                    MinorVersion: protocolParams.Protocol_minor_ver.Value,
                    MinFeeA: protocolParams.Min_fee_a.Value,
                    MinFeeB: protocolParams.Min_fee_b.Value,
                    CoinsPerUtxoWord: uint.Parse(protocolParams.Coins_per_utxo_word)));
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            _instrumentor.TrackDependency(
                EventIds.NetworkContextRetrievalElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(BlockfrostNetworkContextRetriever),
                nameof(BlockfrostClient),
                nameof(GetNetworkContext),
                isSuccessful: true);
        }
    }
}
