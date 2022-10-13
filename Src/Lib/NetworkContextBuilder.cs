using CardanoSharp.Koios.Sdk.Contracts;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public static class NetworkContextBuilder
{
    public static NetworkContext Build(BlockSummary latestBlock, ProtocolParameters protocolParams)
    {
        return new NetworkContext(
            (uint)latestBlock.AbsSlot,
            new ProtocolParams(
                MajorVersion: protocolParams.ProtocolMajor,
                MinorVersion: protocolParams.ProtocolMinor,
                MinFeeA: protocolParams.MinFeeA,
                MinFeeB: protocolParams.MinFeeB,
                CoinsPerUtxoWord: protocolParams.CoinsPerUtxoWord));
    }

}
