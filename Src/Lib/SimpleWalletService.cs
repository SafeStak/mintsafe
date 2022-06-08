using CardanoSharp.Koios.Sdk;
using CardanoSharp.Koios.Sdk.Contracts;
using CardanoSharp.Wallet.Encoding;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.Scripts;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.Scripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using PeterO.Cbor2;
using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public interface ISimpleWalletService
{
    Task<string?> SubmitTransactionAsync(
        string sourcePaymentAddress,
        string sourcePaymentSkey,
        Network network,
        NativeAssetValue[]? nativeAssetsToMint = null,
        PendingTransactionOutput[]? outputs = null,
        Dictionary<int, Dictionary<string, object>>? metadata = null,
        string? withdrawalStakeSkey = null,
        string[]? policySkey = null,
        uint? policyExpirySlot = null,
        CancellationToken ct = default);
}

public class SimpleWalletService : ISimpleWalletService
{
    private readonly ILogger<SimpleWalletService> _logger;
    private readonly IInstrumentor _instrumentor;

    public SimpleWalletService(
        ILogger<SimpleWalletService> logger,
        IInstrumentor instrumentor)
    {
        _logger = logger;
        _instrumentor = instrumentor;
    }

    public async Task<string?> SubmitTransactionAsync(
        string sourcePaymentAddress,
        string sourcePaymentSkey,
        Network network,
        NativeAssetValue[]? nativeAssetsToMint = null,
        PendingTransactionOutput[]? outputs = null,
        Dictionary<int, Dictionary<string, object>>? metadata = null,
        string? withdrawalStakeSkey = null,
        string[]? policySkeys = null,
        uint? policyExpirySlot = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        (var epochClient, var networkClient, var addressClient, var txClient) = GetKoiosClients(network);
        var tipTask = networkClient.GetChainTip().ConfigureAwait(false);
        var sourceAddressInfoTask = addressClient.GetAddressInformation(sourcePaymentAddress).ConfigureAwait(false);
        var tipResponse = await tipTask;
        var addrInfoResponse = await sourceAddressInfoTask;
        if (tipResponse == null || !tipResponse.IsSuccessStatusCode || tipResponse.Content == null
            || addrInfoResponse == null || !addrInfoResponse.IsSuccessStatusCode
            || addrInfoResponse.Content == null || addrInfoResponse.Content.Length == 0)
        {
            _logger.LogWarning("Koios responses from tip and addressinfo are null, empty or have unsuccessful status codes");
            return null;
        }
        var tip = tipResponse.Content.First();
        var protocolParamsResponse = await epochClient.GetProtocolParameters(tip.Epoch.ToString());
        if (protocolParamsResponse == null || !protocolParamsResponse.IsSuccessStatusCode || protocolParamsResponse.Content == null)
        {
            _logger.LogWarning("Koios responses from protocol params is null or have unsuccessful status codes");
            return null;
        }
        _logger.LogInformation(
            "Queried Koios {elapsedMs}ms - Epoch: {Epoch}, AbsSlot: {AbsSlot}, SourceAddressUtxoCount: {SourceAddressUtxoCount}",
            sw.ElapsedMilliseconds, tip.Epoch, tip.AbsSlot, addrInfoResponse.Content.Length);

        var sourceAddressUtxos = BuildSourceAddressUtxos(addrInfoResponse.Content);
        // Inputs TODO: Coin selection?
        var txInputs = sourceAddressUtxos; 
        var consolidatedInputValue = BuildConsolidatedTxInputValue(sourceAddressUtxos, nativeAssetsToMint);
        // Outputs
        var txOutputs = outputs ?? Array.Empty<PendingTransactionOutput>();
        var consolidatedOutputValue = txOutputs.Select(txOut => txOut.Value).Sum();
        var txChangeOutput = consolidatedInputValue.Subtract(consolidatedOutputValue);

        // Start building transaction body using CardanoSharp
        var txBodyBuilder = TransactionBodyBuilder.Create
            .SetFee(0)
            .SetTtl((uint)tip.AbsSlot + 7200);
        // TxInputs
        foreach (var txInput in txInputs)
        {
            txBodyBuilder.AddInput(txInput.TxHash, txInput.OutputIndex);
        }
        // TxOutputs
        foreach (var txOutput in txOutputs)
        {
            var tokenBundleBuilder = (txOutput.Value.NativeAssets.Length > 0)
                ? GetTokenBundleBuilderFromNativeAssets(txOutput.Value.NativeAssets)
                : null;
            txBodyBuilder.AddOutput(new Address(txOutput.Address), txOutput.Value.Lovelaces, tokenBundleBuilder);
        }
        // Build Output Change back to source address
        var minUtxoLovelace = TxUtils.CalculateMinUtxoLovelace(txChangeOutput, (int)protocolParamsResponse.Content.Single().CoinsPerUtxoWord);
        if (txChangeOutput.Lovelaces < minUtxoLovelace) 
        {
            throw new ApplicationException($"Change output does not meet minimum UTxO lovelace requirement of {minUtxoLovelace}");
        }
        txBodyBuilder.AddOutput(new Address(sourcePaymentAddress), txChangeOutput.Lovelaces, GetTokenBundleBuilderFromNativeAssets(txChangeOutput.NativeAssets));
        // TxMint
        if (nativeAssetsToMint != null && nativeAssetsToMint.Length > 0)
        {
            // Build Cardano Native Assets from TestResults
            var freshMintTokenBundleBuilder = TokenBundleBuilder.Create;
            foreach (var newAssetMint in nativeAssetsToMint)
            {
                freshMintTokenBundleBuilder = freshMintTokenBundleBuilder
                    .AddToken(newAssetMint.PolicyId.HexToByteArray(), newAssetMint.AssetName.HexToByteArray(), 1);
            }
            txBodyBuilder.SetMint(freshMintTokenBundleBuilder);
        }
        // TxWitnesses
        var paymentSkey = GetPrivateKeyFromBech32SigningKey(sourcePaymentSkey);
        var witnesses = TransactionWitnessSetBuilder.Create
            .AddVKeyWitness(paymentSkey.GetPublicKey(false), paymentSkey);
        if (policySkeys != null && policySkeys.Any())
        {
            foreach (var policySKey in policySkeys)
            {
                var policyKey = GetPrivateKeyFromBech32SigningKey(policySKey);
                witnesses.AddVKeyWitness(policyKey.GetPublicKey(false), policyKey);
            }
            var policyScriptAllBuilder = GetScriptAllBuilder(policySkeys.Select(GetPrivateKeyFromBech32SigningKey), policyExpirySlot);
            witnesses.SetNativeScript(policyScriptAllBuilder);
        }
        // Build Tx for fee calculation
        var txBuilder = TransactionBuilder.Create
            .SetBody(txBodyBuilder)
            .SetWitnesses(witnesses);
        // Metadata
        var auxDataBuilder = AuxiliaryDataBuilder.Create;
        if (metadata != null && metadata.Any())
        {
            var tag = metadata.Keys.First();
            auxDataBuilder = auxDataBuilder.AddMetadata(tag, metadata[tag]);
            txBuilder = txBuilder.SetAuxData(auxDataBuilder);
            _logger.LogInformation("Build Metadata {txMetadata}", auxDataBuilder);
        }
        // Calculate and update change Utxo
        var protocolParams = protocolParamsResponse.Content.Single();
        var tx = txBuilder.Build();
        var fee = tx.CalculateFee(protocolParams.MinFeeA, protocolParams.MinFeeB);
        txBodyBuilder.SetFee(fee);
        tx.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;
        var txBytes = tx.Serialize();
        var txHash = HashUtility.Blake2b256(tx.TransactionBody.Serialize(auxDataBuilder.Build())).ToStringHex();
        _logger.LogInformation("Built mint tx {elapnsed}ms", sw.ElapsedMilliseconds);

        // Submit Tx
        try
        {
            sw.Restart();
            using var stream = new MemoryStream(txBytes);
            var response = await txClient.Submit(stream).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                _logger.LogWarning("Failed tx submission status={statusCode}, error={errorContent}", response.Error.StatusCode, response.Error.Content);
                return null;
            }
            var txId = response.Content.TrimStart('"').TrimEnd('"');
            if (txId != txHash)
            {
                _logger.LogWarning("TxId {txId} from txClient.Submit is different to calculated TxHash {txHash}", txId, txHash);
            }
            _logger.LogInformation("Submitted mint tx {elapnsed}ms TxId: {txId} ({txBytesLength}bytes)", sw.ElapsedMilliseconds, txId, txBytes.Length);
            return txId;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Failed tx submission {error}", ex.Content);
            return null;
        }
    }

    private static UnspentTransactionOutput[] BuildSourceAddressUtxos(AddressInformation[] addrInfoResponse)
    {
        return addrInfoResponse.Single().UtxoSets
            .Select(utxo => new UnspentTransactionOutput(
                utxo.TxHash,
                utxo.TxIndex,
                new AggregateValue(
                    ulong.Parse(utxo.Value),
                    utxo.AssetList.Select(
                        a => new NativeAssetValue(
                            a.PolicyId,
                            a.AssetName,
                            ulong.Parse(a.Quantity)))
                    .ToArray())))
            .ToArray();
    }

    private static AggregateValue BuildConsolidatedTxInputValue(
        UnspentTransactionOutput[] sourceAddressUtxos,
        NativeAssetValue[]? nativeAssetsToMint)
    {
        if (nativeAssetsToMint != null && nativeAssetsToMint.Length > 0)
        {
            return sourceAddressUtxos
                .Select(utxo => utxo.Value)
                .Concat(new[] { new AggregateValue(0, nativeAssetsToMint) })
                .Sum();
        }
        return sourceAddressUtxos.Select(utxo => utxo.Value).Sum();
    }

    private static ITokenBundleBuilder? GetTokenBundleBuilderFromNativeAssets(NativeAssetValue[] nativeAssets)
    {
        if (nativeAssets.Length == 0)
            return null;
        
        var tokenBundleBuilder = TokenBundleBuilder.Create;
        foreach (var nativeAsset in nativeAssets)
        {
            tokenBundleBuilder = tokenBundleBuilder.AddToken(
                nativeAsset.PolicyId.HexToByteArray(),
                nativeAsset.AssetName.HexToByteArray(),
                nativeAsset.Quantity);
        }
        return tokenBundleBuilder;
    }

    private static (
        IEpochClient epochClient,
        INetworkClient networkClient,
        IAddressClient addressClient,
        ITransactionClient transactionClient
        ) GetKoiosClients(Network network) =>
        (GetBackendClient<IEpochClient>(network),
        GetBackendClient<INetworkClient>(network),
        GetBackendClient<IAddressClient>(network),
        GetBackendClient<ITransactionClient>(network));

    public static T GetBackendClient<T>(Network networkType) =>
        RestService.For<T>(GetBaseUrlForNetwork(networkType));

    private static string GetBaseUrlForNetwork(Network networkType) => networkType switch
    {
        Network.Mainnet => "https://api.koios.rest/api/v0",
        Network.Testnet => "https://testnet.koios.rest/api/v0",
        _ => throw new ArgumentException($"{nameof(networkType)} {networkType} is invalid", nameof(networkType))
    };

    private static PrivateKey GetPrivateKeyFromBech32SigningKey(string bech32EncodedSigningKey)
    {
        var keyBytes = Bech32.Decode(bech32EncodedSigningKey, out _, out _);
        return new PrivateKey(keyBytes[..64], keyBytes[64..]);
    }

    private static IScriptAllBuilder GetScriptAllBuilder(
        IEnumerable<PrivateKey> policySKeys, ulong? policyExpiry = null)
    {
        var scriptAllBuilder = ScriptAllBuilder.Create;
        if (policyExpiry.HasValue)
        {
            scriptAllBuilder.SetScript(
                NativeScriptBuilder.Create.SetInvalidAfter((uint)policyExpiry.Value));
        }
        foreach (var policySKey in policySKeys)
        {
            var policyVKey = policySKey.GetPublicKey(false);
            var policyVKeyHash = HashUtility.Blake2b224(policyVKey.Key);
            scriptAllBuilder = scriptAllBuilder.SetScript(
                NativeScriptBuilder.Create.SetKeyHash(policyVKeyHash));
        }
        return scriptAllBuilder;
    }

    //public async Task<string> SendAllAsync(
    //    string sourceAddress,
    //    string destinationAddress,
    //    string[] message,
    //    string sourceAddressSigningkeyCborHex,
    //    CancellationToken ct = default)
    //{
    //    var paymentId = Guid.NewGuid();

    //    var sw = Stopwatch.StartNew();
    //    var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{nameof(_utxoRetriever.GetUtxosAtAddressAsync)} completed with {utxosAtSourceAddress.Length} after {sw.ElapsedMilliseconds}ms");

    //    var combinedUtxoValues = utxosAtSourceAddress.SelectMany(u => u.Values)
    //        .GroupBy(uv => uv.Unit)
    //        .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
    //        .ToArray();

    //    // Generate payment message metadata 
    //    sw.Restart();
    //    var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
    //    var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
    //    await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

    //    // Generate signing key
    //    var skeyFileName = $"{paymentId}.skey";
    //    var skeyPath = Path.Combine(_settings.BasePath, skeyFileName);
    //    var cliKeyObject = new
    //    {
    //        type = "PaymentSigningKeyShelley_ed25519",
    //        description = "Payment Signing Key",
    //        cborHex = sourceAddressSigningkeyCborHex
    //    };
    //    await File.WriteAllTextAsync(skeyPath, JsonSerializer.Serialize(cliKeyObject)).ConfigureAwait(false);
    //    _logger.LogDebug($"Generated yolo signing key at {skeyPath} for {sourceAddress} after {sw.ElapsedMilliseconds}ms");

    //    var txBuildCommand = new TxBuildCommand(
    //        utxosAtSourceAddress,
    //        new[] { new TxBuildOutput(destinationAddress, combinedUtxoValues, IsFeeDeducted: true) },
    //        Mint: Array.Empty<Value>(),
    //        MintingScriptPath: string.Empty,
    //        MetadataJsonPath: metadataJsonPath,
    //        TtlSlot: 0,
    //        new[] { skeyPath });

    //    sw.Restart();
    //    var submissionPayload = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{_txBuilder.GetType()}{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

    //    sw.Restart();
    //    var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{_txSubmitter.GetType()}{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
    //    _instrumentor.TrackDependency(
    //        EventIds.PaymentElapsed,
    //        sw.ElapsedMilliseconds,
    //        DateTime.UtcNow,
    //        nameof(SimpleWalletService),
    //        string.Empty,
    //        nameof(SendAllAsync),
    //        data: JsonSerializer.Serialize(txBuildCommand),
    //        isSuccessful: true);
    //    return txHash;
    //}

    //public async Task<string> SendValuesAsync(
    //    string sourceAddress,
    //    string destinationAddress,
    //    Value[] values,
    //    string[] message,
    //    string sourceAddressSigningkeyCborHex,
    //    CancellationToken ct = default)
    //{
    //    var paymentId = Guid.NewGuid();

    //    var sw = Stopwatch.StartNew();
    //    var utxosAtSourceAddress = await _utxoRetriever.GetUtxosAtAddressAsync(sourceAddress, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{nameof(_utxoRetriever.GetUtxosAtAddressAsync)} completed with {utxosAtSourceAddress.Length} after {sw.ElapsedMilliseconds}ms");

    //    // Validate source address has the values
    //    var combinedAssetValues = utxosAtSourceAddress.SelectMany(u => u.Values)
    //        .GroupBy(uv => uv.Unit)
    //        .Select(uvg => new Value(Unit: uvg.Key, Quantity: uvg.Sum(u => u.Quantity)))
    //        .ToArray();
    //    foreach (var valueToSend in values)
    //    {
    //        var combinedUnitValue = combinedAssetValues.FirstOrDefault(u => u.Unit == valueToSend.Unit);
    //        if (combinedUnitValue == default)
    //            throw new ArgumentException($"{nameof(values)} utxo does not exist at source address");
    //        if (combinedUnitValue.Quantity < valueToSend.Quantity)
    //            throw new ArgumentException($"{nameof(values)} quantity in source address insufficient for payment");
    //    }

    //    // Generate payment message metadata 
    //    sw.Restart();
    //    var metadataJsonFileName = $"metadata-payment-{paymentId}.json";
    //    var metadataJsonPath = Path.Combine(_settings.BasePath, metadataJsonFileName);
    //    await _metadataGenerator.GenerateMessageMetadataJsonFile(message, metadataJsonPath, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{nameof(_metadataGenerator.GenerateMessageMetadataJsonFile)} generated at {metadataJsonPath} after {sw.ElapsedMilliseconds}ms");

    //    sw.Restart();
    //    var skeyFileName = $"{paymentId}.skey";
    //    var skeyPath = Path.Combine(_settings.BasePath, skeyFileName);
    //    var cliKeyObject = new
    //    {
    //        type = "PaymentSigningKeyShelley_ed25519",
    //        description = "Payment Signing Key",
    //        cborHex = sourceAddressSigningkeyCborHex
    //    };
    //    await File.WriteAllTextAsync(skeyPath, JsonSerializer.Serialize(cliKeyObject)).ConfigureAwait(false);
    //    _logger.LogDebug($"Generated yolo signing key at {skeyPath} for {sourceAddress} after {sw.ElapsedMilliseconds}ms");

    //    // Determine change and build Tx
    //    var changeValues = combinedAssetValues.SubtractValues(values);
    //    var txBuildCommand = new TxBuildCommand(
    //        utxosAtSourceAddress,
    //        new[] {
    //                new TxBuildOutput(destinationAddress, values),
    //                new TxBuildOutput(sourceAddress, changeValues, IsFeeDeducted: true),
    //        },
    //        Mint: Array.Empty<Value>(),
    //        MintingScriptPath: string.Empty,
    //        MetadataJsonPath: metadataJsonPath,
    //        TtlSlot: 0,
    //        new[] { skeyPath });

    //    sw.Restart();
    //    var submissionPayload = await _txBuilder.BuildTxAsync(txBuildCommand, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{_txBuilder.GetType()}{nameof(_txBuilder.BuildTxAsync)} completed after {sw.ElapsedMilliseconds}ms");

    //    sw.Restart();
    //    var txHash = await _txSubmitter.SubmitTxAsync(submissionPayload, ct).ConfigureAwait(false);
    //    _logger.LogDebug($"{_txSubmitter.GetType()}{nameof(_txSubmitter.SubmitTxAsync)} completed after {sw.ElapsedMilliseconds}ms");
    //    _instrumentor.TrackDependency(
    //        EventIds.PaymentElapsed,
    //        sw.ElapsedMilliseconds,
    //        DateTime.UtcNow,
    //        nameof(SimpleWalletService),
    //        string.Empty,
    //        nameof(SendValuesAsync),
    //        data: JsonSerializer.Serialize(txBuildCommand),
    //        isSuccessful: true);
    //    return txHash;
    //}
}
