using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly IMintingKeychainRetriever _keychainRetriever;
    private readonly IMetadataFileGenerator _metadataGenerator;
    private readonly ITxSubmitter _txSubmitter;
    private readonly ITransactionBuilder _txBuilder;

    public UtxoRefunder(
        ILogger<UtxoRefunder> logger,
        MintsafeAppSettings settings,
        ITxInfoRetriever txRetriever,
        IMintingKeychainRetriever keychainRetriever,
        IMetadataFileGenerator metadataGenerator,
        ITransactionBuilder txBuilder,
        ITxSubmitter txSubmitter)
    {
        _logger = logger;
        _settings = settings;
        _txRetriever = txRetriever;
        _keychainRetriever = keychainRetriever;
        _metadataGenerator = metadataGenerator;
        _txBuilder = txBuilder;
        _txSubmitter = txSubmitter;
    }

    public async Task<string> ProcessRefundForUtxo(
        UnspentTransactionOutput utxo,
        SaleContext saleContext,
        NetworkContext networkContext,
        string reason,
        CancellationToken ct = default)
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

        var mintingKeychain = await _keychainRetriever.GetMintingKeyChainAsync(saleContext, ct).ConfigureAwait(false);
        var txRefundCommand = new BuildTransactionCommand(
            Inputs: new[] { utxo },
            Outputs: new[] { new PendingTransactionOutput(buyerAddress, utxo.Value) },
            Mint: Array.Empty<Mint>(),
            Metadata: MetadataBuilder.BuildMessageMetadata($"mintsafe.io refund {reason}"),
            Network: _settings.Network,
            PaymentSigningKeys: mintingKeychain.SigningKeys);

        sw.Restart();
        var tx = _txBuilder.BuildTx(txRefundCommand, networkContext);
        _logger.LogDebug($"{nameof(_txBuilder.BuildTx)} completed after {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var txHash = await _txSubmitter.SubmitTxAsync(tx.Bytes, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
        _logger.LogDebug($"TxID:{txHash} Successfully refunded {utxo.Lovelaces} to {buyerAddress}");

        return txHash;
    }
}
