using SimpleExec;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxBuilder
    {
        private readonly NiftyLaunchpadSettings _settings;

        public TxBuilder(NiftyLaunchpadSettings settings)
        {
            _settings = settings;
        }

        //public async Task<byte[]> BuildTx(TxBuildCommand buildCommand)
        //{
        //    try
        //    {
        //        var stopwatch = Stopwatch.StartNew();
        //        var rawUtxoTable = await Command.ReadAsync(
        //            "cardano-cli", string.Join(" ",
        //                "query", "utxo",
        //                GetNetworkParameter(),
        //                "--address", address
        //            ), noEcho: true);
        //        Console.WriteLine($"UTxOs retrieved after {stopwatch.ElapsedMilliseconds}ms:{Environment.NewLine}{rawUtxoTable}");
        //    }
        //    catch
        //    {

        //    }

        //    return Array.Empty<byte>;
        //}

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }
}
