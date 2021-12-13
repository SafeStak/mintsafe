using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mintsafe.Abstractions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Mintsafe.Lib.UnitTests.FakeGenerator;

namespace Mintsafe.Lib.UnitTests;

public class NiftyDistributorShould
{
    private readonly NiftyDistributor _distributor;
    private readonly Mock<IMetadataFileGenerator> _mockMetadataGenerator;
    private readonly Mock<ITxInfoRetriever> _mockTxIoRetriever;
    private readonly Mock<ITxBuilder> _mockTxBuilder;
    private readonly Mock<ITxSubmitter> _mockTxSubmitter;
    private readonly Mock<ISaleAllocationStore> _mockSaleContextStore;

    public NiftyDistributorShould()
    {
        _mockMetadataGenerator = new Mock<IMetadataFileGenerator>();
        _mockTxIoRetriever = new Mock<ITxInfoRetriever>();
        _mockTxBuilder = new Mock<ITxBuilder>();
        _mockTxSubmitter = new Mock<ITxSubmitter>();
        _mockSaleContextStore = new Mock<ISaleAllocationStore>();
        _distributor = new NiftyDistributor(
            NullLogger<NiftyDistributor>.Instance,
            NullInstrumentor.Instance,
            GenerateSettings(),
            _mockMetadataGenerator.Object,
            _mockTxIoRetriever.Object,
            _mockTxBuilder.Object,
            _mockTxSubmitter.Object,
            _mockSaleContextStore.Object);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public async Task Distribute_Nifties_For_SalePurchase_Given_Active_Sale_When_Purchase_Is_Valid(
        int niftyCount)
    {
        var purchaseAttempt = new PurchaseAttempt(Guid.NewGuid(), Guid.NewGuid(), GenerateUtxos(1, 10000000).First(), 3, 0);
        var allocatedNifties = GenerateTokens(niftyCount).ToArray();
        var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        _mockTxIoRetriever
            .Setup(t => t.GetTxInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateTxIoAggregate());
        _mockTxBuilder
            .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildTxOutputBytes);
        _mockTxSubmitter
            .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
            .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");

        var distributionResult = await _distributor.DistributeNiftiesForSalePurchase(
            nfts: allocatedNifties,
            purchaseAttempt: purchaseAttempt,
            saleContext: GenerateSaleContext());

        distributionResult.Outcome.Should().Be(NiftyDistributionOutcome.Successful);
        distributionResult.PurchaseAttempt.Should().Be(purchaseAttempt);
        distributionResult.NiftiesDistributed.Should().BeEquivalentTo(allocatedNifties);
        distributionResult.MintTxHash.Should().Be("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
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
            .Setup(t => t.GetTxInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateTxIoAggregate());
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
            nfts: GenerateTokens(3).ToArray(),
            purchaseAttempt: new PurchaseAttempt(Guid.NewGuid(), Guid.NewGuid(), purchaseUtxo, 3, 0),
            saleContext: GenerateSaleContext());

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
            .Setup(t => t.GetTxInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateTxIoAggregate(inputAddress: buyerAddress));
        _mockTxBuilder
            .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildTxOutputBytes);
        _mockTxSubmitter
            .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
            .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
        var nifties = GenerateTokens(niftyCount).ToArray();

        var txHash = await _distributor.DistributeNiftiesForSalePurchase(
            nfts: nifties,
            purchaseAttempt: new PurchaseAttempt(
                Guid.NewGuid(), Guid.NewGuid(), GenerateUtxos(1, 10000000).First(), niftyCount, changeInLovelace),
            saleContext: GenerateSaleContext(collection: GenerateCollection(policyId: policyId)));

        _mockTxBuilder
            .Verify(
                t => t.BuildTxAsync(
                    It.Is<TxBuildCommand>(b => IsBuyerOutputCorrect(b, buyerAddress, changeInLovelace, nifties, policyId)),
                    It.IsAny<CancellationToken>()),
                Times.Once,
                "Output should have buyer address and correct values");
    }

    [Theory]
    [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 0.1, "addr_test1qrup2zgu69knkts7m3y3ghhwdzmgaus5u3s28vcsdegajr9wv6zerpdre7qdyvf68dcjyslazq0tfj5rq80v02tm5mysd3xc2u", 10000000, 0, 1)]
    [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 0.078, "addr_test1qp06h7um737tlp2s5um8fvwef5rmx6jh7auchrcwgct3w43w7gtapvad5hzgkvr3ksnzpu6a2ejaew5ypeurygknqs5qhjuvf6", 36000000, 2000000, 3)]
    [InlineData("addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x", 0.0088, "addr_test1vrldgv89yh0edkuwrvkkhc3yx4npfccdvtz7dfkn85a78rsu9nkm4", 50500000, 2500000, 5)]
    public async Task Build_Correct_Tx_Output_For_Creator_Address_Given_Active_Sale_When_Purchase_Is_Valid(
        string creatorAddress, double margin, string buyerAddress, long purchaseLovelaces, int changeInLovelace, int niftyCount)
    {
        var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        var txInfo = GenerateTxIoAggregate(inputAddress: buyerAddress);
        _mockTxIoRetriever
            .Setup(t => t.GetTxInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(txInfo);
        _mockTxBuilder
            .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildTxOutputBytes);
        _mockTxSubmitter
            .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
            .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
        var nifties = GenerateTokens(niftyCount).ToArray();

        var txHash = await _distributor.DistributeNiftiesForSalePurchase(
            nfts: nifties,
            purchaseAttempt: new PurchaseAttempt(
                Guid.NewGuid(), Guid.NewGuid(), GenerateUtxos(1, purchaseLovelaces).First(), niftyCount, changeInLovelace),
            saleContext: GenerateSaleContext(
                sale: GenerateSale(creatorAddress: creatorAddress, postPurchaseMargin: (decimal)margin)));

        _mockTxBuilder
            .Verify(
                t => t.BuildTxAsync(
                    It.Is<TxBuildCommand>(
                        b => IsCreatorOutputCorrect(b, creatorAddress, margin, buyerAddress, purchaseLovelaces, changeInLovelace)),
                    It.IsAny<CancellationToken>()),
                Times.Once,
                "Output should have proceeds address and correct lovelace value");
    }

    [Theory]
    [InlineData("addr_test1vpjvftua27afux73wpjz8089d2fsdu097apcuhdewyxmfssj0dlty", 0.1, "addr_test1qrup2zgu69knkts7m3y3ghhwdzmgaus5u3s28vcsdegajr9wv6zerpdre7qdyvf68dcjyslazq0tfj5rq80v02tm5mysd3xc2u", 10000000, 0, 1)]
    [InlineData("addr_test1vzze0x09pe5v80sxtzz06uvt7gdmdpp9z4m5xndacy4044g8err8c", 0.078,"addr_test1qp06h7um737tlp2s5um8fvwef5rmx6jh7auchrcwgct3w43w7gtapvad5hzgkvr3ksnzpu6a2ejaew5ypeurygknqs5qhjuvf6", 36000000, 2000000, 3)]
    [InlineData("addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x", 0.0088, "addr_test1vrldgv89yh0edkuwrvkkhc3yx4npfccdvtz7dfkn85a78rsu9nkm4", 50500000, 2500000, 5)]
    public async Task Build_Correct_Tx_Output_For_Proceeds_Address_Given_Active_Sale_When_Purchase_Is_Valid(
        string proceedsAddress, double margin, string buyerAddress, long purchaseLovelaces, int changeInLovelace, int niftyCount)
    {
        var buildTxOutputBytes = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        var txInfo = GenerateTxIoAggregate(inputAddress: buyerAddress);
        _mockTxIoRetriever
            .Setup(t => t.GetTxInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(txInfo);
        _mockTxBuilder
            .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildTxOutputBytes);
        _mockTxSubmitter
            .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
            .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
        var allocatedNifties = GenerateTokens(niftyCount).ToArray();
        var sale = GenerateSale(proceedsAddress: proceedsAddress, postPurchaseMargin: (decimal)margin);

        var txHash = await _distributor.DistributeNiftiesForSalePurchase(
            nfts: allocatedNifties,
            purchaseAttempt: new PurchaseAttempt(
                Guid.NewGuid(), Guid.NewGuid(), GenerateUtxos(1, purchaseLovelaces).First(), niftyCount, changeInLovelace),
            saleContext: GenerateSaleContext(sale: sale));

        _mockTxBuilder
            .Verify(
                t => t.BuildTxAsync(
                    It.Is<TxBuildCommand>(
                        b => IsProceedsOutputCorrect(b, proceedsAddress, margin, buyerAddress, purchaseLovelaces, changeInLovelace)),
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
            .Setup(t => t.GetTxInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateTxIoAggregate());
        _mockTxBuilder
            .Setup(t => t.BuildTxAsync(It.IsAny<TxBuildCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buildTxOutputBytes);
        _mockTxSubmitter
            .Setup(t => t.SubmitTxAsync(It.Is<byte[]>(b => b == buildTxOutputBytes), It.IsAny<CancellationToken>()))
            .ReturnsAsync("01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279");
        var nifties = GenerateTokens(niftyCount).ToArray();

        var txHash = await _distributor.DistributeNiftiesForSalePurchase(
            nfts: nifties,
            purchaseAttempt: new PurchaseAttempt(
                Guid.NewGuid(), Guid.NewGuid(), GenerateUtxos(1, 1000000).First(), 3, 0),
            saleContext: GenerateSaleContext(collection: GenerateCollection(policyId: policyId)));

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
        var buyerOutput = buildCommand.Outputs.First(output => output.Address == buyerAddress);
        var buyerOutputLovelace = buyerOutput.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;

        var minUtxoLovelaceQuantity = TxUtils.CalculateMinUtxoLovelace(buyerOutput.Values);
        var expectedNiftyAssetNames = nifties.Select(n => $"{policyId}.{n.AssetName}").ToArray();
        var allSingleNiftyOutputs = buyerOutput.Values
            .Where(v => v.Unit != Assets.LovelaceUnit)
            .All(v => expectedNiftyAssetNames.Contains(v.Unit) && v.Quantity == 1);

        return (buyerOutputLovelace == changeInLovelace + minUtxoLovelaceQuantity) && allSingleNiftyOutputs;
    }

    private static bool IsProceedsOutputCorrect(
        TxBuildCommand buildCommand,
        string proceedsAddress,
        double margin,
        string buyerAddress,
        long purchaseLovelaces,
        long changeInLovelace)
    {
        var buyerOutput = buildCommand.Outputs.First(output => output.Address == buyerAddress);
        var proceedsOutput = buildCommand.Outputs.First(output => output.Address == proceedsAddress);
        var proceedsOutputLovelaces = proceedsOutput.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;
        var minUtxoLovelaces = TxUtils.CalculateMinUtxoLovelace(buyerOutput.Values);

        var saleLovelaces = purchaseLovelaces - changeInLovelace - minUtxoLovelaces;
        var proceedsCutLovelaces = (int)(saleLovelaces * margin);

        return proceedsOutputLovelaces == proceedsCutLovelaces;
    }

    private static bool IsCreatorOutputCorrect(
        TxBuildCommand buildCommand,
        string creatorAddress,
        double margin,
        string buyerAddress,
        long purchaseLovelaces,
        long changeInLovelace)
    {
        var buyerOutput = buildCommand.Outputs.First(output => output.Address == buyerAddress);
        var creatorOutput = buildCommand.Outputs.First(output => output.Address == creatorAddress);
        var creatorOutputLovelaces = creatorOutput.Values.First(v => v.Unit == Assets.LovelaceUnit).Quantity;
        var minUtxoLovelaces = TxUtils.CalculateMinUtxoLovelace(buyerOutput.Values);

        var saleLovelaces = purchaseLovelaces - changeInLovelace - minUtxoLovelaces;
        var proceedsCutLovelaces = (int)(saleLovelaces * margin);

        return creatorOutputLovelaces == saleLovelaces - proceedsCutLovelaces;
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
