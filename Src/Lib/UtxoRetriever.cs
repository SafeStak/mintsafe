using SimpleExec;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class UtxoRetriever 
    {
        private readonly NiftyLaunchpadSettings _settings;

        public UtxoRetriever(NiftyLaunchpadSettings settings)
        {
            _settings = settings;
        }

        public async Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var rawUtxoTable = await Command.ReadAsync(
                    "cardano-cli", string.Join(" ",
                        "query", "utxo",
                        GetNetworkParameter(),
                        "--address", address
                    ), noEcho: true);
                Console.WriteLine($"UTxOs retrieved after {stopwatch.ElapsedMilliseconds}ms:{Environment.NewLine}{rawUtxoTable}");

                var lines = rawUtxoTable.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                var utxos = new Utxo[lines.Length - 2];
                var insertionIndex = 0;
                // TODO: Factor other assets
                foreach (var utxoLine in lines[2..])
                {
                    //Console.WriteLine($"Found Line {index}: {utxoLine}");
                    var contentSegments = utxoLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    utxos[insertionIndex++] = new Utxo(
                        contentSegments[0],
                        int.Parse(contentSegments[1]),
                        new[] { new UtxoValue("lovelace", long.Parse(contentSegments[2])) });
                    //var segmentIndex = 0;
                    //foreach (var contentSegment in contentSegments)
                    //{
                    //    Console.WriteLine($"Segment[{segmentIndex++}]: {contentSegment}");
                    //}
                }
                return utxos;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot run CLI, reverting to fake UTxOs");
                //Console.Error.WriteLine(ex);
                await Task.Delay(1000, ct);
                return new[]
                {
                    new Utxo(
                        "127745e23b81a5a5e22a409ce17ae8672b234dda7be1f09fc9e3a11906bd3a11",
                        0,
                        new [] { new UtxoValue("lovelace", 1000000000) }),
                    //new Utxo(
                    //    "2032080672d43e6cdf8ade97fc1bf839effe1be45434e0e304ea1e538c0e3721",
                    //    2,
                    //    new [] { new UtxoValue("lovelace", 1000000000) }),
                    //new Utxo(
                    //    "5611a9a5846caeda54e3aa5c7f6b9f26c639fd60be601d7fbc918d1e9a241500",
                    //    5,
                    //    new [] { new UtxoValue("lovelace", 1000000000) }),
                };
            }
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }

    public class FakeUtxoRetriever
    {
        private readonly NiftyLaunchpadSettings _settings;

        public FakeUtxoRetriever(NiftyLaunchpadSettings settings)
        {
            _settings = settings;
        }

        public async Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            await Task.Delay(1000, ct);
            var utxos = Array.Empty<Utxo>();

            return new[] {
                new Utxo(
                    "127745e23b81a5a5e22a409ce17ae8672b234dda7be1f09fc9e3a11906bd3a11",
                    0,
                    new[] { new UtxoValue("lovelace", 10000000) }),
            };
        }
    }
}
