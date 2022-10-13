using Microsoft.Extensions.Logging.Abstractions;
using Mintsafe.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Mintsafe.Lib.UnitTests.FakeGenerator;

namespace Mintsafe.Lib.UnitTests;

public class GenericMintingServiceShould
{
    //[Fact(Skip = "Live integration test - can fail if run concurrently with other tests")]
    [Fact]
    public async Task Submit_Mint_Transaction_Successfully_When_Consolidating_Own_Address_Utxos()
    {
        var mockTxSubmitter = new Mock<ITxSubmitter>();
        var simpleWalletService = new GenericMintingService(
            NullLogger<GenericMintingService>.Instance,
            NullInstrumentor.Instance,
            new CardanoSharpTxBuilder(
                NullLogger<CardanoSharpTxBuilder>.Instance,
                NullInstrumentor.Instance,
                GenerateSettings()),
                mockTxSubmitter.Object);

        var fromAddress = "addr_test1qq5zuhh9685fup86syuzmu3e6eengzv8t46mfqxg086cvqz8fquadv00d7t7a88rlf6z2knwfesls5f2cndan7runlcsad62ju";
        string toAddress = "addr_test1qpvttg5263dnutj749k5dcr35yk5mr94fxx0q2zs2xeuxq5hvcrpf2ezgxucdwcjytcrww34j5y609ss4sfpptg3uvpsxmcdtf";
        uint lovelaces = 51455855;
        var skey = "addr_xsk1fzw9r482t0ekua7rcqewg3k8ju5d9run4juuehm2p24jtuzz4dg4wpeulnqhualvtx9lyy7u0h9pdjvmyhxdhzsyy49szs6y8c9zwfp0eqyrqyl290e6dr0q3fvngmsjn4aask9jjr6q34juh25hczw3euust0dw";
        var network = Network.Testnet;

        var tx = await simpleWalletService.MintNativeAssets(
            from: fromAddress, 
            to: toAddress, 
            network: network, 
            balanceToSend: new Balance(lovelaces, Array.Empty<NativeAssetValue>()), 
            fromSigningKey: skey);

    }
}
