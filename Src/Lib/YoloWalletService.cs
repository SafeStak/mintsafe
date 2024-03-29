﻿using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

/// <summary>
/// A TOTALLY UNSAFE wallet API used for quicker testnet validation.
/// The signing keys behind the source address are passed in raw hex format
/// Take the cboxHex field value after running "cat payment.skey"
/// </summary>
public interface IYoloWalletService
{
    Task<string> SendAllAsync(
        string sourceAddress,
        string destinationAddress,
        string[] message,
        string sourceAddressSigningkeyCborHex,
        CancellationToken ct = default);

    Task<string> SendValuesAsync(
        string sourceAddress,
        string destinationAddress,
        Value[] values,
        string[] message,
        string sourceAddressSigningkeyCborHex,
        CancellationToken ct = default);
}

public class YoloWalletService : IYoloWalletService
{
    private readonly ILogger<YoloWalletService> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly IUtxoRetriever _utxoRetriever;
    private readonly IMetadataFileGenerator _metadataGenerator;
    private readonly ITxSubmitter _txSubmitter;
    private readonly ITxBuilder _txBuilder;

    public YoloWalletService(
        ILogger<YoloWalletService> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        IUtxoRetriever utxoRetriever,
        IMetadataFileGenerator metadataGenerator,
        ITxBuilder txBuilder,
        ITxSubmitter txSubmitter)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _utxoRetriever = utxoRetriever;
        _metadataGenerator = metadataGenerator;
        _txBuilder = txBuilder;
        _txSubmitter = txSubmitter;
    }

    public async Task<string> SendAllAsync(
        string sourceAddress,
        string destinationAddress,
        string[] message,
        string sourceAddressSigningkeyCborHex,
        CancellationToken ct = default)
    {
        var paymentId = Guid.NewGuid();

        var sw = Stopwatch.StartNew();
        var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_utxoRetriever.GetUtxosAtAddressAsync)} completed with {utxosAtSourceAddress.Length} after {sw.ElapsedMilliseconds}ms");

        var combinedUtxoValues = utxosAtSourceAddress.SelectMany(u => u.Values)
            .GroupBy(uv => uv.Unit)
            .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
            .ToArray();

        // Generate payment message metadata 
        sw.Restart();
        var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
        var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
        await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

        // Generate signing key
        var skeyFileName = $"{paymentId}.skey";
        var skeyPath = Path.Combine(_settings.BasePath, skeyFileName);
        var cliKeyObject = new
        {
            type = "PaymentSigningKeyShelley_ed25519",
            description = "Payment Signing Key",
            cborHex = sourceAddressSigningkeyCborHex
        };
        await File.WriteAllTextAsync(skeyPath, JsonSerializer.Serialize(cliKeyObject)).ConfigureAwait(false);
        _logger.LogDebug($"Generated yolo signing key at {skeyPath} for {sourceAddress} after {sw.ElapsedMilliseconds}ms");

        var txBuildCommand = new TxBuildCommand(
            utxosAtSourceAddress,
            new[] { new TxBuildOutput(destinationAddress, combinedUtxoValues, IsFeeDeducted: true) },
            Mint: Array.Empty<Value>(),
            MintingScriptPath: string.Empty,
            MetadataJsonPath: metadataJsonPath,
            TtlSlot: 0,
            new[] { skeyPath });

        sw.Restart();
        var submissionPayload = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
        _logger.LogDebug($"{_txBuilder.GetType()}{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct).ConfigureAwait(false);
        _logger.LogDebug($"{_txSubmitter.GetType()}{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
        _instrumentor.TrackDependency(
            EventIds.PaymentElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(YoloWalletService),
            string.Empty,
            nameof(SendAllAsync),
            data: JsonSerializer.Serialize(txBuildCommand),
            isSuccessful: true);
        return txHash;
    }

    public async Task<string> SendValuesAsync(
        string sourceAddress,
        string destinationAddress,
        Value[] values,
        string[] message,
        string sourceAddressSigningkeyCborHex,
        CancellationToken ct = default)
    {
        var paymentId = Guid.NewGuid();

        var sw = Stopwatch.StartNew();
        var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_utxoRetriever.GetUtxosAtAddressAsync)} completed with {utxosAtSourceAddress.Length} after {sw.ElapsedMilliseconds}ms");

        // Validate source address has the values
        var combinedAssetValues = utxosAtSourceAddress.SelectMany(u => u.Values)
            .GroupBy(uv => uv.Unit)
            .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
            .ToArray();
        foreach (var valueToSend in values)
        {
            var combinedUnitValue = combinedAssetValues.FirstOrDefault(u => u.Unit == valueToSend.Unit);
            if (combinedUnitValue == default)
                throw new ArgumentException($"{nameof(values)} utxo does not exist at source address");
            if (combinedUnitValue.Quantity < valueToSend.Quantity)
                throw new ArgumentException($"{nameof(values)} quantity in source address insufficient for payment");
        }

        // Generate payment message metadata 
        sw.Restart();
        var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
        var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
        await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct).ConfigureAwait(false);
        _logger.LogDebug($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var skeyFileName = $"{paymentId}.skey";
        var skeyPath = Path.Combine(_settings.BasePath, skeyFileName);
        var cliKeyObject = new
        {
            type = "PaymentSigningKeyShelley_ed25519",
            description = "Payment Signing Key",
            cborHex = sourceAddressSigningkeyCborHex
        };
        await File.WriteAllTextAsync(skeyPath, JsonSerializer.Serialize(cliKeyObject)).ConfigureAwait(false);
        _logger.LogDebug($"Generated yolo signing key at {skeyPath} for {sourceAddress} after {sw.ElapsedMilliseconds}ms");

        // Determine change and build Tx
        var changeValues = combinedAssetValues.SubtractValues(values);
        var txBuildCommand = new TxBuildCommand(
            utxosAtSourceAddress,
            new[] {
                    new TxBuildOutput(destinationAddress, values),
                    new TxBuildOutput(sourceAddress, changeValues, IsFeeDeducted: true),
            },
            Mint: Array.Empty<Value>(),
            MintingScriptPath: string.Empty,
            MetadataJsonPath: metadataJsonPath,
            TtlSlot: 0,
            new[] { skeyPath });

        sw.Restart();
        var submissionPayload = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
        _logger.LogDebug($"{_txBuilder.GetType()}{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

        sw.Restart();
        var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct).ConfigureAwait(false);
        _logger.LogDebug($"{_txSubmitter.GetType()}{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
        _instrumentor.TrackDependency(
            EventIds.PaymentElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(YoloWalletService),
            string.Empty,
            nameof(SendValuesAsync),
            data: JsonSerializer.Serialize(txBuildCommand),
            isSuccessful: true);
        return txHash;
    }

 
}
