using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using SimpleExec;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class CardanoCliUtxoRetriever : IUtxoRetriever
    {
        private readonly ILogger<CardanoCliUtxoRetriever> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly string _networkMagic;

        public CardanoCliUtxoRetriever(
            ILogger<CardanoCliUtxoRetriever> logger,
            MintsafeSaleWorkerSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _networkMagic = _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }

        public async Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var rawUtxoResponse = string.Empty;
            try
            {
                rawUtxoResponse = await Command.ReadAsync(
                    "cardano-cli", string.Join(" ",
                        "query", "utxo",
                        _networkMagic,
                        "--address", address
                    ), noEcho: true, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                throw new CardanoCliException("cardano-cli exception", ex, _settings.Network.ToString());
            }
            finally
            {
                _logger.LogInformation($"UTxOs retrieved after {sw.ElapsedMilliseconds}ms:{Environment.NewLine}{rawUtxoResponse}");
            }

            var lines = rawUtxoResponse.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            var utxos = new Utxo[lines.Length - 2];
            var insertionIndex = 0;
            foreach (var utxoLine in lines[2..])
            {
                //_logger.LogInformation($"Found Line {index}: {utxoLine}");
                // Every utxo line is formatted like
                // {TxHash} {TxOutputIndex} {LovelaceValue} lovelaces [+ {CustomTokenValue} {PolicyId}.{AssetName}] + TxDatumHashNone
                var contentSegments = utxoLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lovelaceValue = new UtxoValue("lovelace", long.Parse(contentSegments[2]));

                // Debugging other assets
                var segmentIndex = 5;
                foreach (var contentSegment in contentSegments[5..])
                {
                    _logger.LogInformation($"Segment[{segmentIndex++}]: {contentSegment}");
                }
                var bits = string.Join(string.Empty, contentSegments[5..]);
                _logger.LogInformation($"Other values: {bits}");

                utxos[insertionIndex++] = new Utxo(
                    TxHash: contentSegments[0],
                    OutputIndex: int.Parse(contentSegments[1]),
                    Values: new[] { lovelaceValue });

            }
            return utxos;
        }
    }

    public class FakeUtxoRetriever : IUtxoRetriever
    {
        public async Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            await Task.Delay(1000, ct);
            return new[] {
                new Utxo(
                    "127745e23b81a5a5e22a409ce17ae8672b234dda7be1f09fc9e3a11906bd3a11",
                    0,
                    new[] { new UtxoValue("lovelace", 15000000) }),
            };
        }
    }
}
