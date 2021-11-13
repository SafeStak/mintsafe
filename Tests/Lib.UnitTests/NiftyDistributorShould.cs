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

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async Task Distribute_Nifties_For_SalePurchase_Given_Active_Sale_When_Purchase_Is_Valid(
            int niftyCount)
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
                nfts: Generator.GenerateTokens(niftyCount).ToArray(),
                purchaseRequest: new PurchaseAttempt(Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, 10000000).First(), 3, 0),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale());

            txHash.Should().Be("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(
                        It.Is<TxBuildCommand>(b => b.Mint.Length == niftyCount),
                        It.IsAny<CancellationToken>()),
                    Times.Once,
                    "Output should have buyer address and correct values");
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
        [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 0, 1, "0ac248e17f0fc35be4d2a7d186a84cdcda5b88d7ad2799ebe98a98b2")]
        [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 1000000, 3, "629718e24d22e0c02c2efd27290e1a58ebc2972635a7c523aee2d8fc")]
        public async Task Build_Correct_Tx_Output_For_Buyer_SalePurchase_Given_Active_Sale_When_Purchase_Is_Valid(
            string buyerAddress, int changeInLovelace, int niftyCount, string policyId)
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
            var nifties = Generator.GenerateTokens(niftyCount).ToArray();

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: nifties,
                purchaseRequest: new PurchaseAttempt(
                    Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, 10000000).First(), niftyCount, changeInLovelace),
                collection: Generator.GenerateCollection(policyId: policyId),
                sale: Generator.GenerateSale());

            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(
                        It.Is<TxBuildCommand>(b => IsBuyerOutputCorrect(b, buyerAddress, changeInLovelace, nifties, policyId)), 
                        It.IsAny<CancellationToken>()),
                    Times.Once, 
                    "Output should have buyer address and correct values");
        }

        [Theory]
        [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 10000000, 0, 1)]
        [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 10000000, 2000000, 3)]
        public async Task Build_Correct_Tx_Output_For_Proceeds_Address_Given_Active_Sale_When_Purchase_Is_Valid(
            string proceedsAddress, long purchaseLovelaces, int changeInLovelace, int niftyCount)
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
            var nifties = Generator.GenerateTokens(niftyCount).ToArray();

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: nifties,
                purchaseRequest: new PurchaseAttempt(
                    Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, purchaseLovelaces).First(), niftyCount, changeInLovelace),
                collection: Generator.GenerateCollection(),
                sale: Generator.GenerateSale(proceedsAddress: proceedsAddress));

            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(
                        It.Is<TxBuildCommand>(
                            b => IsProceedsOutputCorrect(b, proceedsAddress, purchaseLovelaces, changeInLovelace)), 
                        It.IsAny<CancellationToken>()),
                    Times.Once,
                    "Output should have proceeds address and correct lovelace value");
        }

        [Theory]
        [InlineData(1, "0ac248e17f0fc35be4d2a7d186a84cdcda5b88d7ad2799ebe98a98b2")]
        [InlineData(3, "629718e24d22e0c02c2efd27290e1a58ebc2972635a7c523aee2d")]
        public async Task Build_Correct_Tx_Mint_For_Nifities_Given_Active_Sale_When_Purchase_Is_Valid(
            int niftyCount, string policyId)
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
            var nifties = Generator.GenerateTokens(niftyCount).ToArray();

            var txHash = await _distributor.DistributeNiftiesForSalePurchase(
                nfts: nifties,
                purchaseRequest: new PurchaseAttempt(
                    Guid.NewGuid(), Guid.NewGuid(), Generator.GenerateUtxos(1, 1000000).First(), 3, 0),
                collection: Generator.GenerateCollection(policyId: policyId),
                sale: Generator.GenerateSale());

            _mockTxBuilder
                .Verify(
                    t => t.BuildTxAsync(
                        It.Is<TxBuildCommand>(b => IsMintCorrect(b, nifties, policyId)), It.IsAny<CancellationToken>()),
                    Times.Once,
                    "Should have correctly mapped mint parameters for nifties");
        }

        private static bool IsBuyerOutputCorrect(
            TxBuildCommand buildCommand, 
            string buyerAddress, 
            long changeInLovelace, 
            Nifty[] nifties, 
            string policyId)
        {
            var output = buildCommand.Outputs.First(output => output.Address == buyerAddress);
            var outputLovelace = output.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;

            var expectedNiftyAssetNames = nifties.Select(n => $"{policyId}.{n.AssetName}").ToArray();
            var allSingleNiftyOutputs = output.Values
                .Where(v => v.Unit != Assets.LovelaceUnit)
                .All(v => expectedNiftyAssetNames.Contains(v.Unit) && v.Quantity == 1);

            return (outputLovelace == changeInLovelace + 2000000) && allSingleNiftyOutputs;
        }

        private static bool IsProceedsOutputCorrect(
            TxBuildCommand buildCommand, 
            string proceedsAddress, 
            long purchaseLovelaces, 
            long changeInLovelace)
        {
            var output = buildCommand.Outputs.First(output => output.Address == proceedsAddress);
            var outputLovelace = output.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;

            return outputLovelace == purchaseLovelaces - changeInLovelace - 2000000;
        }

        private static bool IsMintCorrect(
            TxBuildCommand buildCommand,
            Nifty[] nifties,
            string policyId)
        {
            var expectedNiftyAssetNames = nifties.Select(n => $"{policyId}.{n.AssetName}").ToArray();
            var allSingleNiftyMints = buildCommand.Mint
                .All(v => expectedNiftyAssetNames.Contains(v.Unit) && v.Quantity == 1);

            return allSingleNiftyMints;
        }
    }
}
