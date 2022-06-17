using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using SimpleExec;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class CardanoCliUtxoRetriever : IUtxoRetriever
{
    private readonly ILogger<CardanoCliUtxoRetriever> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly string _networkMagic;

    public CardanoCliUtxoRetriever(
        ILogger<CardanoCliUtxoRetriever> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _networkMagic = _settings.Network == Network.Mainnet
            ? "--mainnet"
            : "--testnet-magic 1097911063";
    }

    public async Task<UnspentTransactionOutput[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
    {
        var isSuccessful = false;
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
            isSuccessful = true;
        }
        catch (FormatException ex)
        {
            throw new CardanoCliException("cardano-cli returned an invalid response", ex, _settings.Network.ToString());
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
            _instrumentor.TrackDependency(
                EventIds.UtxoRetrievalElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(CardanoCliUtxoRetriever),
                address,
                nameof(GetUtxosAtAddressAsync),
                isSuccessful: isSuccessful);
            _logger.LogDebug($"UTxOs retrieval finished after {sw.ElapsedMilliseconds}ms:{Environment.NewLine}{rawUtxoResponse}");
        }

        var lines = rawUtxoResponse.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var utxos = new UnspentTransactionOutput[lines.Length - 2];
        var insertionIndex = 0;
        foreach (var utxoLine in lines[2..]) // skip the headers
        {
            var contentSegments = utxoLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var values = ParseValues(contentSegments);

            utxos[insertionIndex++] = new UnspentTransactionOutput(
                TxHash: contentSegments[0],
                OutputIndex: uint.Parse(contentSegments[1]),
                Value: values);

        }
        return utxos;
    }

    private static AggregateValue ParseValues(string[] utxoLineSegments)
    {
        // Must always contain an ADA/lovelace UTXO value
        var lovelaceValue = ulong.Parse(utxoLineSegments[2]);

        var nativeAssets = new List<NativeAssetValue>();
        var currentSegmentIndex = 4; // 4 comes frrom skipping [0]{txHash} [1]{txOutputIndex} [2]{txOutputLovelaceValue} [3]lovelace
        while (utxoLineSegments[currentSegmentIndex] == "+" && utxoLineSegments[currentSegmentIndex + 1] != "TxOutDatumNone")
        {
            //_logger.LogDebug($"FOUND {utxoLineSegments[currentSegmentIndex]} AND {utxoLineSegments[currentSegmentIndex + 1]}");
            var quantity = ulong.Parse(utxoLineSegments[currentSegmentIndex + 1]);
            var unit = utxoLineSegments[currentSegmentIndex + 2];
            var unitParts = unit.Split('.');
            nativeAssets.Add(new NativeAssetValue(unitParts[0], unitParts[1], quantity));
            currentSegmentIndex += 3; // skip "+ {quantity} {unit}"
        }
        return new AggregateValue(lovelaceValue, nativeAssets.ToArray());
    }
}

public class FakeUtxoRetriever : IUtxoRetriever
{
    public async Task<UnspentTransactionOutput[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
    {
        await Task.Delay(1000, ct).ConfigureAwait(false);
        return GenerateUtxos(3,
            45_000000,
            10_000000,
            60_000000);
    }

    private static UnspentTransactionOutput[] GenerateUtxos(int count, params ulong[] values)
    {
        if (values.Length != count)
            throw new ArgumentException($"{nameof(values)} must be the same length as count", nameof(values));

        return Enumerable.Range(0, count)
            .Select(i => new UnspentTransactionOutput(
                "127745e23b81a5a5e22a409ce17ae8672b234dda7be1f09fc9e3a11906bd3a11",
                (uint)i,
                new AggregateValue(values[i], Array.Empty<NativeAssetValue>())))
            .ToArray();
    }
}
