using System.Threading.Tasks;
using Mintsafe.Abstractions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using System.Threading;
using FluentAssertions;

namespace Mintsafe.Lib.UnitTests
{
    public class BlockfrostUtxoRetrieverShould
    {
        private readonly BlockfrostUtxoRetriever _bfUtxoRetriever;
        private readonly Mock<IBlockfrostClient> _mockBlockfrostClient;

        public BlockfrostUtxoRetrieverShould()
        {
            _mockBlockfrostClient = new Mock<IBlockfrostClient>();
            _bfUtxoRetriever = new BlockfrostUtxoRetriever(NullLogger<BlockfrostUtxoRetriever>.Instance, _mockBlockfrostClient.Object);
        }

        [Theory]
        [InlineData("d29bbb14dbe448eda8156f5439335ce6c800f39f4812dfc6f8293274871d6e52", 0U, "540f107c7a3df20d2111a41c3bc407cce3e63c10c8dd673d51a02c22434f4e4431", "1", "2172289",
            "540f107c7a3df20d2111a41c3bc407cce3e63c10c8dd673d51a02c22", "434f4e4431", 1, 2172289)]
        public async Task Should_Map_Utxo_Values_Correctly(
            string bfTxHash, uint bfOutputIndex, string bfNativeAssetUnit, string bfNativeAssetQuantity, string bfLovelaceQuantity,
            string expectedNativeAssetPolicyId, string expectedNativeAssetName, ulong expectedNativeAssetQuantity, ulong expectedLovelaceQuantity)
        {
            _mockBlockfrostClient.Setup(m => m.GetUtxosAtAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {
                    new BlockFrostAddressUtxo
                    {
                        Tx_hash = bfTxHash,
                        Output_index = bfOutputIndex,
                        Amount = new[]
                        {
                            new BlockFrostValue
                            {
                                Unit = "lovelace",
                                Quantity = bfLovelaceQuantity
                            },
                            new BlockFrostValue
                            {
                                Unit = bfNativeAssetUnit,
                                Quantity = bfNativeAssetQuantity
                            },
                        }
                    } 
                });

            var utxos = await _bfUtxoRetriever.GetUtxosAtAddressAsync("addr1qy3y89nnzdqs4fmqv49fmpqw24hjheen3ce7tch082hh6x7nwwgg06dngunf9ea4rd7mu9084sd3km6z56rqd7e04ylslhzn9h", CancellationToken.None);

            Assert.NotNull(utxos);
            utxos.Length.Should().Be(1);
            var utxo = utxos[0];
            utxo.Lovelaces.Should().Be(expectedLovelaceQuantity);
            utxo.Value.Lovelaces.Should().Be(expectedLovelaceQuantity);
            utxo.Value.NativeAssets[0].PolicyId.Should().Be(expectedNativeAssetPolicyId);
            utxo.Value.NativeAssets[0].AssetName.Should().Be(expectedNativeAssetName);
            utxo.Value.NativeAssets[0].Quantity.Should().Be(expectedNativeAssetQuantity);
        }
    }
}
