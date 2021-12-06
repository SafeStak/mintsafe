using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class UtxoRefunder : IUtxoRefunder
{
    private const long MinLovelace = 1000000;

    private readonly ILogger<UtxoRefunder> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly ITxInfoRetriever _txRetriever;
    private readonly IMetadataFileGenerator _metadataGenerator;
    private readonly ITxSubmitter _txSubmitter;
    private readonly ITxBuilder _txBuilder;

    public UtxoRefunder(
        ILogger<UtxoRefunder> logger,
        MintsafeAppSettings settings,
        ITxInfoRetriever txRetriever,
        IMetadataFileGenerator metadataGenerator,
        ITxBuilder txBuilder,
        ITxSubmitter txSubmitter)
    {
        _logger = logger;
        _settings = settings;
        _txRetriever = txRetriever;
        _metadataGenerator = metadataGenerator;
        _txBuilder = txBuilder;
        _txSubmitter = txSubmitter;
    }

    public async Task<string> ProcessRefundForUtxo(
        Utxo utxo, string signingKeyFilePath, string reason, CancellationToken ct = default)
    {
        _logger.LogDebug($"Processing refund for {utxo} with {utxo.Lovelaces}lovelaces ({reason})");

        if (utxo.Lovelaces < MinLovelace)
        {
            _logger.LogWarning($"Cannot refund {utxo.Lovelaces} because of minimum Utxo lovelace value requirement ({MinLovelace})");
            return string.Empty;
        }
        
        var sw = Stopwatch.StartNew();
        var txIo = await _txRetriever.GetTxInfoAsync(utxo.TxHash, ct).ConfigureAwait(false);
        var buyerAddress = txIo.Inputs.First().Address;
        _logger.LogDebug($"{nameof(_txRetriever.GetTxInfoAsync)} completed after {sw.ElapsedMilliseconds}ms");

        // Generate refund message metadata 
        var metadataJsonFileName = $"metadata-refund-{utxo}.json";
        var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
        var message = new[] {
                $"mintsafe.io refund",
                utxo.TxHash,
                $"#{utxo.OutputIndex}",
                reason
            };
        sw.Restart();
        await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

        var txRefundCommand = new TxBuildCommand(
            Inputs: new[] { utxo },
            Outputs: new[] { new TxBuildOutput(buyerAddress, utxo.Values, IsFeeDeducted: true) },
            Mint: Array.Empty<Value>(),
            MintingScriptPath: string.Empty,
            MetadataJsonPath: metadataJsonPath,
            TtlSlot: 0,
            SigningKeyFiles: new[] { signingKeyFilePath });

        sw.Restart();
        var submissionPayload = await _txBuilder.BuildTxAsync(txRefundCommand, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
        _logger.LogDebug($"TxID:{txHash} Successfully refunded {utxo.Lovelaces} to {buyerAddress}");

        return txHash;
    }
}
