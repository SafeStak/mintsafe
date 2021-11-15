using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using SimpleExec;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

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
        catch (Win32Exception ex)
        {
            throw new CardanoCliException("cardano-cli does not exist", ex, _settings.Network.ToString());
        }
        catch (Exception ex)
        {
            throw new CardanoCliException("cardano-cli unhandled exception", ex, _settings.Network.ToString());
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
            var lovelaceValue = new Value(Assets.LovelaceUnit, long.Parse(contentSegments[2]));

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
        return GenerateUtxos(3,
            15000000,
            10000000,
            30000000);
    }

    private static Utxo[] GenerateUtxos(int count, params long[] values)
    {
        if (values.Length != count)
            throw new ArgumentException($"{nameof(values)} must be the same length as count", nameof(values));

        return Enumerable.Range(0, count)
            .Select(i => new Utxo(
                "127745e23b81a5a5e22a409ce17ae8672b234dda7be1f09fc9e3a11906bd3a11",
                i,
                new[] { new Value(Assets.LovelaceUnit, values[i]) }))
            .ToArray();
    }
}
