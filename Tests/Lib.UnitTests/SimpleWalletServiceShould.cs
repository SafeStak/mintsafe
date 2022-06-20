using CardanoSharp.Wallet.Encoding;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.Scripts;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.Scripts;
using CardanoSharp.Wallet.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using static Mintsafe.Lib.UnitTests.FakeGenerator;

namespace Mintsafe.Lib.UnitTests;

public class SimpleWalletServiceShould
{
    private const int MessageMetadataStandardKey = 674;
    private const int NftRoyaltyMetadataStandardKey = 777;

    [Fact(Skip = "Live integration test - can fail if run concurrently with other tests")]
    public async Task Submit_Transaction_Successfully_When_Consolidating_Own_Address_Utxos()
    {
        var simpleWalletService = new SimpleWalletService(NullLogger<SimpleWalletService>.Instance, NullInstrumentor.Instance);
        var sourcePaymentAddress = "addr_test1qrlsqwhg4vay0x4yy6lw08s4qx4kcpnujcc7dz8e5lqjdnkdfj93s4scmnax7lgemz2q2ftms4zna9y9xle0c3c88f5qhvdm5m";
        var sourcePaymentXsk = "addr_xsk1gr99v0ynu0wvuxwe4vu2sap3d3vxqzpupu93gvhtu869wmk8wezu2zlkssy9xj4j5u5ymf356np04f2tt77pfees3uckytdlpdhv8pwss378smdhsecy7un46q8738rhgwd0hytz4r77k6v95dmt47g0ms4ec63v";
        var network = Network.Testnet;
        var messageBodyMetadata = new Dictionary<string, object>
            { { "msg", new[] { "mintsafe.io test", DateTime.UtcNow.ToString("o") } } };
        var messageMetadata = new Dictionary<int, Dictionary<string, object>>
            { { MessageMetadataStandardKey, messageBodyMetadata } };

        var txId = await simpleWalletService.SubmitTransactionAsync(
            sourcePaymentAddress,
            sourcePaymentXsk,
            network,
            metadata: messageMetadata);

        Assert.NotNull(txId);
    }

    [Fact(Skip = "Live integration test - can fail if run concurrently with other tests")]
    public async Task Submit_Transaction_Successfully_When_Making_Simple_Ada_Payment_To_One_Address()
    {
        var simpleWalletService = new SimpleWalletService(NullLogger<SimpleWalletService>.Instance, NullInstrumentor.Instance);
        var sourcePaymentAddress = "addr_test1qrlsqwhg4vay0x4yy6lw08s4qx4kcpnujcc7dz8e5lqjdnkdfj93s4scmnax7lgemz2q2ftms4zna9y9xle0c3c88f5qhvdm5m";
        var sourcePaymentXsk = "addr_xsk1gr99v0ynu0wvuxwe4vu2sap3d3vxqzpupu93gvhtu869wmk8wezu2zlkssy9xj4j5u5ymf356np04f2tt77pfees3uckytdlpdhv8pwss378smdhsecy7un46q8738rhgwd0hytz4r77k6v95dmt47g0ms4ec63v";
        var network = Network.Testnet;
        // Not used now
        var destinationPaymentAddress = "addr_test1qplxcfvad2uzq2w4k99unzj6d5hmpprgrujn3l0nwsl8vh3e2mgaxpeslac7hghtxxzcwerr3wt6ly2t9hr7unkua9rskg2855";
        var destinationOutputValue = new AggregateValue(8888888, Array.Empty<NativeAssetValue>());
        var messageBodyMetadata = new Dictionary<string, object>
            { { "msg", new[] { "mintsafe.io test", DateTime.UtcNow.ToString("o") } } };
        var messageMetadata = new Dictionary<int, Dictionary<string, object>>
            { { MessageMetadataStandardKey, messageBodyMetadata } };

        var txId = await simpleWalletService.SubmitTransactionAsync(
            sourcePaymentAddress,
            sourcePaymentXsk,
            network,
            outputs: new[] { new PendingTransactionOutput(destinationPaymentAddress, destinationOutputValue) },
            metadata: messageMetadata);

        Assert.NotNull(txId);
    }

    [Fact(Skip = "Live integration test - can fail if run concurrently with other tests")]
    public async Task Submit_Transaction_Successfully_When_Minting_Nft_Royalty_Asset()
    {
        var simpleWalletService = new SimpleWalletService(NullLogger<SimpleWalletService>.Instance, NullInstrumentor.Instance);
        var sourcePaymentAddress = "addr_test1qrlsqwhg4vay0x4yy6lw08s4qx4kcpnujcc7dz8e5lqjdnkdfj93s4scmnax7lgemz2q2ftms4zna9y9xle0c3c88f5qhvdm5m";
        var sourcePaymentXsk = "addr_xsk1gr99v0ynu0wvuxwe4vu2sap3d3vxqzpupu93gvhtu869wmk8wezu2zlkssy9xj4j5u5ymf356np04f2tt77pfees3uckytdlpdhv8pwss378smdhsecy7un46q8738rhgwd0hytz4r77k6v95dmt47g0ms4ec63v";
        var royaltyRate = "0.10";
        var royaltyAddress = "addr_test1qrlsqwhg4vay0x4yy6lw08s4qx4kcpnujcc7dz8e5lqjdnkdfj93s4scmnax7lgemz2q2ftms4zna9y9xle0c3c88f5qhvdm5m";
        var policySkey = "policy_sk1xyz"; 
        var policyExpirySlot = 98504109U;
        var policyId = BuildScriptAllPolicy(policySkey, policyExpirySlot).GetPolicyId().ToStringHex();
        var network = Network.Mainnet;
        var nativeAssetsToMint = new[] { new NativeAssetValue(policyId, "", 1) }; // empty assetname is required for CIP27
        var minUtxoLovelace = TxUtils.CalculateMinUtxoLovelace(nativeAssetsToMint);
        var royaltyBodyMetadata = new Dictionary<string, object>
            { 
                { "rate", royaltyRate }, 
                { "addr", royaltyAddress.Length > 64 ? MetadataJsonBuilder.SplitStringToChunks(royaltyAddress) : royaltyAddress }  
            };
        var royaltyMetadata = new Dictionary<int, Dictionary<string, object>>
            { { NftRoyaltyMetadataStandardKey, royaltyBodyMetadata } };

        var txId = await simpleWalletService.SubmitTransactionAsync(
            sourcePaymentAddress,
            sourcePaymentXsk,
            network,
            outputs: new[] { new PendingTransactionOutput(sourcePaymentAddress, new AggregateValue(minUtxoLovelace, nativeAssetsToMint)) },
            nativeAssetsToMint: nativeAssetsToMint,
            metadata: royaltyMetadata,
            policySkeys: new[] { policySkey },
            policyExpirySlot: policyExpirySlot);

        Assert.NotNull(txId);
    }

    private static NativeScript BuildScriptAllPolicy(
        string policySKey, ulong? policyExpiry = null)
    {
        var scriptAll = new ScriptAll();
        if (policyExpiry.HasValue)
        {
            scriptAll.NativeScripts.Add(
                new NativeScript
                {
                    InvalidAfter = new ScriptInvalidAfter
                    {
                        After = (uint)policyExpiry.Value
                    }
                });
        }
        var policyVKey = GetPrivateKeyFromBech32SigningKey(policySKey).GetPublicKey(false);
        var policyVKeyHash = HashUtility.Blake2b224(policyVKey.Key);
        scriptAll.NativeScripts.Add(
            new NativeScript
            {
                ScriptPubKey = new ScriptPubKey
                {
                    KeyHash = policyVKeyHash
                }
            });
        return new NativeScript { ScriptAll = scriptAll };
    }

    private static PrivateKey GetPrivateKeyFromBech32SigningKey(string bech32EncodedSigningKey)
    {
        var keyBytes = Bech32.Decode(bech32EncodedSigningKey, out _, out _);
        return new PrivateKey(keyBytes[..64], keyBytes[64..]);
    }
}
