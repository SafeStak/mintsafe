using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mintsafe.Lib;

public class CardanoSharpException : ApplicationException
{
    public string Network { get; }
    public BuildTransactionCommand Command { get; }

    public CardanoSharpException(
        string message,
        BuildTransactionCommand command,
        Exception inner,
        string network) : base(message, inner)
    {
        Network = network;
        Command = command;
    }
}

public class CardanoSharpTxBuilder : IMintTransactionBuilder
{
    private const ulong FeePadding = 132;
    private readonly ILogger<CardanoSharpTxBuilder> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;

    public CardanoSharpTxBuilder(
        ILogger<CardanoSharpTxBuilder> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
    }

    public BuiltTransaction BuildTx(
        BuildTransactionCommand buildCommand,
        NetworkContext networkContext)
    {
        var isSuccessful = false;
        var sw = Stopwatch.StartNew();
        try
        {
            var txInputs = buildCommand.Inputs;
            var consolidatedInputValue = BuildConsolidatedTxInputValue(
                txInputs, buildCommand.Mint.SelectMany(m => m.NativeAssetsToMint).ToArray());
            // Outputs
            var txOutputs = buildCommand.Outputs;
            var consolidatedOutputValue = txOutputs.Select(txOut => txOut.Value).Sum();
            var valueDifference = consolidatedInputValue.Subtract(consolidatedOutputValue);
            if (!valueDifference.IsZero())
            {
                throw new InputOutputValueMismatchException(
                    "Input/Output value mismatch", buildCommand.Inputs, buildCommand.Outputs);
            }

            // Start building transaction body using CardanoSharp
            var txBodyBuilder = TransactionBodyBuilder.Create
                .SetFee(0)
                .SetTtl(networkContext.LatestSlot + buildCommand.TtlTipOffsetSlots);
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
            // TxMint
            if (buildCommand.Mint.Length > 0)
            {
                // Build Cardano Native Assets from TestResults
                var freshMintTokenBundleBuilder = TokenBundleBuilder.Create;
                foreach (var newAssetMint in buildCommand.Mint.SelectMany(m => m.NativeAssetsToMint))
                {
                    freshMintTokenBundleBuilder = freshMintTokenBundleBuilder
                        .AddToken(newAssetMint.PolicyId.HexToByteArray(), newAssetMint.AssetName.HexToByteArray(), 1);
                }
                txBodyBuilder.SetMint(freshMintTokenBundleBuilder);
            }
            // TxWitnesses
            var witnesses = TransactionWitnessSetBuilder.Create;
            foreach (var signingKey in buildCommand.PaymentSigningKeys)
            {
                var paymentSkey = TxUtils.GetPrivateKeyFromBech32SigningKey(signingKey);
                witnesses.AddVKeyWitness(paymentSkey.GetPublicKey(false), paymentSkey);
            }
            foreach (var policy in buildCommand.Mint.Select(m => m.BasicMintingPolicy))
            {
                foreach (var policySigningKey in policy.PolicySigningKeysAll)
                {
                    var policyKey = TxUtils.GetPrivateKeyFromBech32SigningKey(policySigningKey);
                    witnesses.AddVKeyWitness(policyKey.GetPublicKey(false), policyKey);
                }
                // Build NativeScript 
                var policyScriptAllBuilder = GetScriptAllBuilder(
                    policy.PolicySigningKeysAll.Select(TxUtils.GetPrivateKeyFromBech32SigningKey),
                    policy.ExpirySlot);
                witnesses.SetScriptAllNativeScript(policyScriptAllBuilder);
            }

            // Build Tx for fee calculation
            var txBuilder = TransactionBuilder.Create
                .SetBody(txBodyBuilder)
                .SetWitnesses(witnesses);
            // Metadata
            var auxDataBuilder = AuxiliaryDataBuilder.Create;
            foreach (var key in buildCommand.Metadata.Keys)
            {
                auxDataBuilder = auxDataBuilder.AddMetadata(key, buildCommand.Metadata[key]);
            }
            txBuilder = txBuilder.SetAuxData(auxDataBuilder);
            _logger.LogInformation("Build Metadata {txMetadata}", auxDataBuilder);
            // Calculate and update change Utxo
            var tx = txBuilder.Build();
            var fee = tx.CalculateFee(networkContext.ProtocolParams.MinFeeA, networkContext.ProtocolParams.MinFeeB) + FeePadding; 
            txBodyBuilder.SetFee(fee);
            tx.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;
            var txBytes = tx.Serialize();
            var txHash = HashUtility.Blake2b256(tx.TransactionBody.Serialize(auxDataBuilder.Build())).ToStringHex();
            _logger.LogInformation("Built mint tx {elapnsed}ms", sw.ElapsedMilliseconds);
            var cborHex = txBytes.ToStringHex();
            isSuccessful = true;
            return new BuiltTransaction(txHash, txBytes);
        }
        catch (Exception ex)
        {
            throw new CardanoSharpException(
                $"Unhandled exception in {nameof(CardanoSharpTxBuilder)}", 
                command: buildCommand,
                ex, 
                _settings.Network.ToString());
        }
        finally
        {
            _instrumentor.TrackDependency(
                EventIds.TxBuilderElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(CardanoSharpTxBuilder),
                nameof(CardanoSharp),
                nameof(BuildTx),
                isSuccessful: isSuccessful);
        }
    }

    public BuiltTransaction BuildTx(BuildTxCommand txCommand, NetworkContext networkContext)
    {
        var isSuccessful = false;
        // Prevent null propagation for optional properties of array type
        var mint = txCommand.Mint ?? Array.Empty<NativeAssetValue>();
        var signingKeys = txCommand.SigningKeys ?? Array.Empty<string>();
        var simpleScripts = txCommand.SimpleScripts ?? Array.Empty<SimpleScript>();
        var rewardsWithdrawals = txCommand.RewardsWithdrawals ?? Array.Empty<RewardsWithdrawal>();
        var sw = Stopwatch.StartNew();
        try
        {
            var consolidatedInputValue = BuildConsolidatedTxInputValue(txCommand.Inputs, mint);
            var consolidatedOutputValue = txCommand.Outputs.Select(txOut => txOut.Value).Sum();
            var valueDifference = consolidatedInputValue.Subtract(consolidatedOutputValue);
            if (!valueDifference.IsZero())
            {
                throw new InputOutputValueMismatchException(
                    "Input/Output value mismatch", txCommand.Inputs, txCommand.Outputs);
            }
            // Start building transaction body using CardanoSharp
            var txBodyBuilder = TransactionBodyBuilder.Create
                .SetFee(networkContext.ProtocolParams.MinFeeB)
                .SetTtl(networkContext.LatestSlot + txCommand.TtlTipOffsetSlots);
            // Inputs
            foreach (var txInput in txCommand.Inputs)
            {
                txBodyBuilder.AddInput(txInput.TxHash, txInput.OutputIndex);
            }
            // Outputs
            foreach (var txOutput in txCommand.Outputs)
            {
                var tokenBundleBuilder = (txOutput.Value.NativeAssets.Length > 0)
                    ? GetTokenBundleBuilderFromNativeAssets(txOutput.Value.NativeAssets)
                    : null;
                txBodyBuilder.AddOutput(new Address(txOutput.Address), txOutput.Value.Lovelaces, tokenBundleBuilder);
            }
            // Mint
            if (mint.Length > 0)
            {
                // Build Cardano Native Assets from TestResults
                var freshMintTokenBundleBuilder = TokenBundleBuilder.Create;
                foreach (var newAssetMint in mint)
                {
                    freshMintTokenBundleBuilder = freshMintTokenBundleBuilder
                        .AddToken(newAssetMint.PolicyId.HexToByteArray(), newAssetMint.AssetName.HexToByteArray(), 1);
                }
                txBodyBuilder.SetMint(freshMintTokenBundleBuilder);
            }
            // Witnesses
            var witnesses = TransactionWitnessSetBuilder.Create;
            foreach (var signingKey in signingKeys)
            {
                var paymentSkey = TxUtils.GetPrivateKeyFromBech32SigningKey(signingKey);
                witnesses.AddVKeyWitness(paymentSkey.GetPublicKey(false), paymentSkey);
            }
            // Simple Scripts
            foreach (var simpleScript in simpleScripts)
            {
                witnesses.AddNativeScript(GetScriptBuilderForSimpleScript(simpleScript));
            }

            // Build Tx for fee calculation
            var txBuilder = TransactionBuilder.Create
                .SetBody(txBodyBuilder)
                .SetWitnesses(witnesses);
            // Metadata
            IAuxiliaryDataBuilder? auxDataBuilder = null;
            if (txCommand.Metadata is not null && txCommand.Metadata.Keys.Any())
            {
                auxDataBuilder = AuxiliaryDataBuilder.Create;
                foreach (var key in txCommand.Metadata.Keys)
                {
                    auxDataBuilder = auxDataBuilder.AddMetadata(key, txCommand.Metadata[key]);
                }
                txBuilder = txBuilder.SetAuxData(auxDataBuilder);
            }
            _logger.LogInformation("Build Metadata {txMetadata}", auxDataBuilder);
            // Calculate and update change Utxo
            var tx = txBuilder.Build();
            var fee = tx.CalculateFee(networkContext.ProtocolParams.MinFeeA, networkContext.ProtocolParams.MinFeeB);
            txBodyBuilder.SetFee(fee);
            tx.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;
            var txBytes = tx.Serialize();
            var txHash = HashUtility.Blake2b256(tx.TransactionBody.Serialize(auxDataBuilder?.Build())).ToStringHex();
            _logger.LogInformation("Built mint tx {elapnsed}ms", sw.ElapsedMilliseconds);
            var cborHex = txBytes.ToStringHex();
            isSuccessful = true;
            return new BuiltTransaction(txHash, txBytes);
        }
        catch 
        {
            throw;
        }
        finally
        {
            _instrumentor.TrackDependency(
                EventIds.TxBuilderElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(CardanoSharpTxBuilder),
                nameof(CardanoSharp),
                nameof(BuildTx),
                isSuccessful: isSuccessful);
        }
    }

    private static Balance BuildConsolidatedTxInputValue(
        UnspentTransactionOutput[] sourceAddressUtxos,
        NativeAssetValue[]? nativeAssetsToMint)
    {
        if (nativeAssetsToMint != null && nativeAssetsToMint.Length > 0)
        {
            return sourceAddressUtxos
                .Select(utxo => utxo.Value)
                .Concat(new[] { new Balance(0, nativeAssetsToMint) })
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


    private static INativeScriptBuilder GetScriptBuilderForSimpleScript(SimpleScript simpleScript) =>
        simpleScript.Type switch
        {
            NativeScriptType.PubKeyHash => NativeScriptBuilder.Create.SetKeyHash(
                simpleScript.PubKeyHash.HexToByteArray()),
            NativeScriptType.InvalidBefore => NativeScriptBuilder.Create.SetInvalidBefore(
                simpleScript.InvalidBefore ?? 0),
            NativeScriptType.InvalidAfter => NativeScriptBuilder.Create.SetInvalidAfter(
                simpleScript.InvalidAfter ?? 0),
            NativeScriptType.Any => NativeScriptBuilder.Create.SetScriptAny(
                simpleScript.Scripts?.Select(GetScriptBuilderForSimpleScript)?.ToArray() ?? Array.Empty<INativeScriptBuilder>()),
            NativeScriptType.All => NativeScriptBuilder.Create.SetScriptAll(
                simpleScript.Scripts?.Select(GetScriptBuilderForSimpleScript)?.ToArray() ?? Array.Empty<INativeScriptBuilder>()),
            NativeScriptType.AtLeast => NativeScriptBuilder.Create.SetScriptNofK(
                simpleScript.AtLeast ?? 0,
                simpleScript.Scripts?.Select(GetScriptBuilderForSimpleScript)?.ToArray() ?? Array.Empty<INativeScriptBuilder>()),
            _ => throw new NotImplementedException()
        };
}
