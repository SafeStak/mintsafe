﻿using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using SimpleExec;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class CardanoCliTxSubmitter : ITxSubmitter
{
    private readonly ILogger<CardanoCliTxSubmitter> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly string _networkMagic;

    public CardanoCliTxSubmitter(
        ILogger<CardanoCliTxSubmitter> logger,
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

    public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
    {
        // Build the JSON containing cborHex that the CLI expects
        var txSubmissionId = Guid.NewGuid();
        var txSignedJsonPath = Path.Combine(_settings.BasePath, $"{txSubmissionId}.txsigned");
        var txSignedJson = $"{{ \"type\": \"Tx MaryEra\", \"description\": \"\", \"cborHex\": \"{Convert.ToHexString(txSignedBinary)}\"}}";
        await File.WriteAllTextAsync(txSignedJsonPath, txSignedJson, ct).ConfigureAwait(false);

        var isSuccessful = false;
        var sw = Stopwatch.StartNew();
        var txHash = string.Empty;
        try
        {
            var rawTxSubmissionResponse = await Command.ReadAsync(
                    "cardano-cli", string.Join(" ",
                        "transaction", "submit",
                        _networkMagic,
                        "--tx-file", txSignedJsonPath
                    ), noEcho: true, cancellationToken: ct);

            // Derive the txhash
            txHash = await Command.ReadAsync(
                "cardano-cli", string.Join(" ",
                    "transaction", "txid",
                    "--tx-file", txSignedJsonPath
                ), noEcho: true, cancellationToken: ct);
            isSuccessful = true;

            return txHash;
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
            _logger.LogDebug($"Tx submitted after {sw.ElapsedMilliseconds}ms:{Environment.NewLine}{txHash}");
            _instrumentor.TrackDependency(
                EventIds.TxSubmissionElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(CardanoCliTxSubmitter),
                txSignedJsonPath, 
                nameof(SubmitTxAsync),
                isSuccessful: isSuccessful);
        }
    }
}
