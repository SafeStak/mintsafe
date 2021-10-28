using SimpleExec;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxBuilder : ITxBuilder
    {
        private readonly NiftyLaunchpadSettings _settings;

        public TxBuilder(NiftyLaunchpadSettings settings)
        {
            _settings = settings;
        }

        public async Task<byte[]> BuildTxAsync(
            TxBuildCommand buildCommand, CancellationToken ct = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                await Task.Delay(100);
                var output = "Job done init";
                //var output = await Command.ReadAsync(
                //    "cardano-cli", string.Join(" ",
                //        "transaction", "build-raw",
                //        "--out-file", buildCommand.TxRawOutputPath
                //    ), noEcho: true);

                Console.WriteLine($"Tx built after {stopwatch.ElapsedMilliseconds}ms:{output}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            return Array.Empty<byte>();
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }
}
