using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mintsafe.Abstractions;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using static Mintsafe.Lib.UnitTests.FakeGenerator;

namespace Mintsafe.Lib.UnitTests;

public class NiftyAllocatorShould
{
    private readonly NiftyAllocator _allocator;
    private readonly Mock<ISaleAllocationStore> _mockSaleContextStore;

    public NiftyAllocatorShould()
    {
        _mockSaleContextStore = new Mock<ISaleAllocationStore>();
        _allocator = new NiftyAllocator(
            NullLogger<NiftyAllocator>.Instance,
            NullInstrumentor.Instance,
            GenerateSettings(),
            _mockSaleContextStore.Object);
    }

    // TODO: Move to SaleAllocationFileStore tests
    //[Theory]
    //[InlineData(1, 0, 1, 1, 1)]
    //[InlineData(500, 0, 2, 500, 2)]
    //[InlineData(127, 373, 2, 500, 2)]
    //[InlineData(5, 495, 5, 500, 5)]
    //[InlineData(10000, 0, 10, 1000, 10)]
    //[InlineData(10000, 900, 10, 1000, 10)]
    //[InlineData(9800, 200, 100, 10000, 100)]
    //public async Task Allocate_Tokens_Correctly_Given_Spare_Mintable_Tokens_And_Sale_Allocations_When_Purchase_Request_Requested_Quantity_Is_Within_Limits(
    //    int saleMintableCount,
    //    int saleAllocatedCount,
    //    int requestedQuantity,
    //    int saleReleaseQuantity,
    //    int expectedAllocatedQuantity)
    //{
    //    var sale = GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
    //    var collection = GenerateCollection();
    //    var mintableTokens = GenerateTokens(saleMintableCount);
    //    var allocatedTokens = GenerateTokens(saleAllocatedCount);
    //    var saleContext = GenerateSaleContext(sale, collection, mintableTokens, allocatedTokens);
    //    var request = new PurchaseAttempt(
    //        Guid.NewGuid(),
    //        Guid.NewGuid(),
    //        new Utxo("", 0, new[] { new Value(Assets.LovelaceUnit, 1000000) }),
    //        requestedQuantity,
    //        0);

    //    var allocated = await _allocator.AllocateNiftiesForPurchaseAsync(
    //        request, saleContext);

    //    allocated.Length.Should().Be(expectedAllocatedQuantity);
    //}

    [Theory]
    [InlineData(1, 1, 1, 1)]
    [InlineData(2, 1, 1, 1)]
    [InlineData(500, 49, 2, 50)]
    [InlineData(1000, 145, 8, 150)]
    [InlineData(5000, 9995, 10, 10000)]
    public async Task Throws_CannotAllocateMoreThanSaleReleaseException_When_Requesting_Exceeds_Sale_Release_Quantity(
        int saleMintableCount,
        int saleAllocatedCount,
        int requestedQuantity,
        int saleReleaseQuantity)
    {
        var sale = GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
        var collection = GenerateCollection();
        var mintableTokens = GenerateTokens(saleMintableCount);
        var allocatedTokens = GenerateTokens(saleAllocatedCount);
        var saleContext = GenerateSaleContext(sale, collection, mintableTokens, allocatedTokens);
        var request = new PurchaseAttempt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UnspentTransactionOutput("", 0, new AggregateValue(1000000, Array.Empty<NativeAssetValue>())),
            requestedQuantity,
            0);

        Func<Task> asyncTask = async () =>
        {
            var allocated = await _allocator.AllocateNiftiesForPurchaseAsync(
                request, saleContext);
        };

        await asyncTask.Should().ThrowAsync<CannotAllocateMoreThanSaleReleaseException>();
    }

    [Theory]
    [InlineData(0, 0, 0, 1)]
    [InlineData(1, 49, -1, 50)]
    [InlineData(10, 140, -8, 150)]
    [InlineData(9860, 140, -100, 10000)]
    public async Task Throws_ArgumentException_When_Requesting_Zero_Or_Negative_Token_Quantity(
        int saleMintableCount,
        int saleAllocatedCount,
        int requestedQuantity,
        int saleReleaseQuantity)
    {
        var sale = GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
        var collection = GenerateCollection();
        var allocatedTokens = GenerateTokens(saleAllocatedCount);
        var mintableTokens = GenerateTokens(saleMintableCount);
        var saleContext = GenerateSaleContext(sale, collection, mintableTokens, allocatedTokens);
        var request = new PurchaseAttempt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UnspentTransactionOutput("", 0, new AggregateValue(1000000, Array.Empty<NativeAssetValue>())),
            requestedQuantity,
            0);

        Func<Task> asyncTask = async () =>
        {
            var allocated = await _allocator.AllocateNiftiesForPurchaseAsync(
                request, saleContext);
        };

        await asyncTask.Should().ThrowAsync<ArgumentException>();
    }
}
