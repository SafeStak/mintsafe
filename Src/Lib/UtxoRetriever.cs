using SimpleExec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                var rawUtxoTable = await Command.ReadAsync(
                    "cardano-cli", string.Join(" ",
                        "query", "utxo",
                        GetNetworkParameter(),
                        "--address", address
                    ), noEcho: true);

                var lines = rawUtxoTable.Split(Environment.NewLine);
                foreach (var utxoLine in lines[2..])
                {
                    Console.WriteLine($"Found LINE: {utxoLine}");

                    var contentSegments = rawUtxoTable.Split('\t');
                    foreach (var contentSegment in contentSegments)
                    {
                        Console.WriteLine($"Found Content: {contentSegment}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            await Task.Delay(3000, ct);

            return new[]
            {
                new Utxo(
                    "768c63e27a1c816a83dc7b07e78af673b2400de8849ea7e7b734ae1333d100d2",
                    0,
                    new [] { new UtxoValue("lovelace",10000000)}),
                new Utxo(
                    "4c4e67bafa15e742c13c592b65c8f74c769cd7d9af04c848099672d1ba391b49",
                    0,
                    new [] { new UtxoValue("lovelace",20000000)}),
                new Utxo(
                    "768c63e27a1c816a83dc7b07e78af673b2400de8849ea7e7b734ae1333d100d2",
                    0,
                    new [] { new UtxoValue("lovelace",30000000)}),
            };
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }
}
