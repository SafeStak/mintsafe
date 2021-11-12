using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mintsafe.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mintsafe.Lib.UnitTests
{
    public class NiftyDistributorShould
    {
        private readonly NiftyDistributor _distributor;
        private readonly Mock<IMetadataGenerator> _mockMetadataGenerator;
        private readonly Mock<ITxIoRetriever> _mockTxIoRetriever;
        private readonly Mock<ITxBuilder> _mockTxBuilder;
        private readonly Mock<ITxSubmitter> _mockTxSubmitter;

        public NiftyDistributorShould()
        {
            _mockMetadataGenerator = new Mock<IMetadataGenerator>();
            _mockTxIoRetriever = new Mock<ITxIoRetriever>();
            _mockTxBuilder = new Mock<ITxBuilder>();
            _mockTxSubmitter = new Mock<ITxSubmitter>();
            _distributor = new NiftyDistributor(
                NullLogger<NiftyDistributor>.Instance,
                Generator.GenerateSettings(),
                _mockMetadataGenerator.Object,
                _mockTxIoRetriever.Object,
                _mockTxBuilder.Object,
                _mockTxSubmitter.Object);
        }

        [Fact]
        public async Task Distribute_Nifties_For_SalePurchase_Given_Active_Sale_When_Purchase_Is_Valid()
        {
            var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            _mockTxIoRetriever
                .Setup(t => t.GetTxIoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Generator.GenerateTxIoAggregate());
            _mockTxBuilder
                .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(buildTxOutputBytes);
            _mockTxSubmitter
                .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
                .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: Generator.GenerateTokens(3).ToArray(),
                purchaseRequest: new PurchaseAttempt(Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, 10000000).First(), 3, 0),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale());

            txHash.Should().Be("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
        }

        [Theory]
        [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 0, 10000000)]
        [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 2, 35000000)]
        public async Task Build_Correct_Tx_Input_For_Buyer_SalePurchase_Given_Active_Sale_When_Purchase_Is_Valid(
            string purchaseTxHash, int purchaseOutputIndex, long purchaseUtxoLovelaceValue)
        {
            var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            _mockTxIoRetriever
                .Setup(t => t.GetTxIoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Generator.GenerateTxIoAggregate());
            _mockTxBuilder
                .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(buildTxOutputBytes);
            _mockTxSubmitter
                .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
                .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
            var purchaseUtxo = new Utxo(
                purchaseTxHash,
                purchaseOutputIndex,
                new[] { new Value(Assets.LovelaceUnit, purchaseUtxoLovelaceValue) });

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: Generator.GenerateTokens(3).ToArray(),
                purchaseRequest: new PurchaseAttempt(Guid.NewGuid(), Guid.NewGuid(), purchaseUtxo, 3, 0),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale());

            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(It.Is<TxBuildCommand>(b => b.Inputs.First() == purchaseUtxo && b.Inputs.Length == 1), It.IsAny<CancellationToken>()),
                    Times.Once,
                    "Input should be the purchase UTxO");
        }

        [Theory]
        [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 0)]
        [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 1000000)]
        public async Task Build_Correct_Tx_Output_For_Buyer_SalePurchase_Given_Active_Sale_When_Purchase_Is_Valid(
            string buyerAddress, int changeInLovelace)
        {
            var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            _mockTxIoRetriever
                .Setup(t => t.GetTxIoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Generator.GenerateTxIoAggregate(inputAddress: buyerAddress));
            _mockTxBuilder
                .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(buildTxOutputBytes);
            _mockTxSubmitter
                .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
                .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: Generator.GenerateTokens(3).ToArray(),
                purchaseRequest: new PurchaseAttempt(
                    Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, 10000000).First(), 3, changeInLovelace),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale());

            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(It.Is<TxBuildCommand>(b => IsBuyerOutputCorrect(b, buyerAddress, changeInLovelace)), It.IsAny<CancellationToken>()),
                    Times.Once, 
                    "Output should have buyer address and correct lovelace value returned");
        }

        [Theory]
        [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 10000000, 0)]
        [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 10000000, 2000000)]
        public async Task Build_Correct_Tx_Output_For_Proceeds_Address_Given_Active_Sale_When_Purchase_Is_Valid(
            string proceedsAddress, long purchaseLovelaces, int changeInLovelace)
        {
            var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            _mockTxIoRetriever
                .Setup(t => t.GetTxIoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Generator.GenerateTxIoAggregate());
            _mockTxBuilder
                .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(buildTxOutputBytes);
            _mockTxSubmitter
                .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
                .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: Generator.GenerateTokens(3).ToArray(),
                purchaseRequest: new PurchaseAttempt(
                    Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, purchaseLovelaces).First(), 3, changeInLovelace),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale(proceedsAddress: proceedsAddress));

            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(It.Is<TxBuildCommand>(b => IsProceedsOutputCorrect(b, proceedsAddress, purchaseLovelaces, changeInLovelace)), It.IsAny<CancellationToken>()),
                    Times.Once,
                    "Output should have proceeds address and correct lovelace value");
        }

        [Fact]
        public async Task Build_Correct_Tx_Mint_For_Nifities_Given_Active_Sale_When_Purchase_Is_Valid()
        {
            var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
            _mockTxIoRetriever
                .Setup(t => t.GetTxIoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Generator.GenerateTxIoAggregate());
            _mockTxBuilder
                .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(buildTxOutputBytes);
            _mockTxSubmitter
                .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
                .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
            var nifties = Generator.GenerateTokens(3).ToArray();

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: nifties,
                purchaseRequest: new PurchaseAttempt(
                    Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, 1000000).First(), 3, 0),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale());

            // TODO: Verify mint and output fields
            //_mockTxBuilder
            //    .Verify(
            //        t => t.BuildTxAsync(It.Is<TxBuildCommand>(b => ??)), It.IsAny<CancellationToken>()),
            //        Times.Once,
            //        "Should have correctly mapped mint parameters for nifties");
        }

        private bool IsBuyerOutputCorrect(
            TxBuildCommand buildCommand, string buyerAddress, long changeInLovelace)
        {
            var output = buildCommand.Outputs.First(output => output.Address == buyerAddress);
            var outputLovelace = output.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;

            return outputLovelace == changeInLovelace + 2000000;
        }

        private bool IsProceedsOutputCorrect(
            TxBuildCommand buildCommand, string proceedsAddress, long purchaseLovelaces, long changeInLovelace)
        {
            var output = buildCommand.Outputs.First(output => output.Address == proceedsAddress);
            var outputLovelace = output.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;

            return outputLovelace == purchaseLovelaces - changeInLovelace - 2000000;
        }
    }
}
