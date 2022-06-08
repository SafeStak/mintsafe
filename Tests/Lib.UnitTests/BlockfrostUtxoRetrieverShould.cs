using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
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
        [InlineData("d29bbb14dbe448eda8156f5439335ce6c800f39f4812dfc6f8293274871d6e52", 0, "540f107c7a3df20d2111a41c3bc407cce3e63c10c8dd673d51a02c22434f4e4431", "1", "2172289",
            "540f107c7a3df20d2111a41c3bc407cce3e63c10c8dd673d51a02c22.434f4e4431", 1, 2172289)]
        public async Task Should_Map_Utxo_Values_Correctly(
            string bfTxHash, int bfOutputIndex, string bfNativeAssetUnit, string bfNativeAssetQuantity, string bfLovelaceQuantity,
            string expectedNativeAssetUnit, long expectedNativeAssetQuantity, long expectedLovelaceQuantity)
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
            utxo.Values[0].Unit.Should().Be("lovelace");
            utxo.Values[0].Quantity.Should().Be(expectedLovelaceQuantity);
            utxo.Values[1].Unit.Should().Be(expectedNativeAssetUnit);
            utxo.Values[1].Quantity.Should().Be(expectedNativeAssetQuantity);
        }
    }
}
