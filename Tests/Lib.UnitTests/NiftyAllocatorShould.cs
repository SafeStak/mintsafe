using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mintsafe.Abstractions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Mintsafe.Lib.UnitTests
{
    public class NiftyAllocatorShould
    {
        private readonly NiftyAllocator _allocator;

        public NiftyAllocatorShould()
        {
            _allocator = new NiftyAllocator(
                NullLogger<NiftyAllocator>.Instance,
                Generator.GenerateSettings());
        }

        [Theory]
        [InlineData(1, 0, 1, 1, 1)]
        [InlineData(500, 0, 2, 500, 2)]
        [InlineData(127, 373, 2, 500, 2)]
        [InlineData(5, 495, 5, 500, 5)]
        [InlineData(10000, 0, 10, 1000, 10)]
        [InlineData(10000, 900, 10, 1000, 10)]
        [InlineData(9800, 200, 100, 10000, 100)]
        public async Task Allocate_Tokens_Correctly_Given_Spare_Mintable_Tokens_And_Sale_Allocations_When_Purchase_Request_Requested_Quantity_Is_Within_Limits(
            int saleMintableCount, 
            int saleAllocatedCount, 
            int requestedQuantity, 
            int saleReleaseQuantity,
            int expectedAllocatedQuantity)
        {
            var sale = Generator.GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
            var mintableTokens = Generator.GenerateTokens(saleMintableCount);
            var allocatedTokens = Generator.GenerateTokens(saleAllocatedCount);
            var request = new PurchaseAttempt(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new Value(Assets.LovelaceUnit, 1000000) }),
                requestedQuantity,
                0);

            var allocated = await _allocator.AllocateTokensForPurchaseAsync(
                request, allocatedTokens, mintableTokens, sale);

            allocated.Length.Should().Be(expectedAllocatedQuantity);
        }

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
            var sale = Generator.GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
            var mintableTokens = Generator.GenerateTokens(saleMintableCount);
            var allocatedTokens = Generator.GenerateTokens(saleAllocatedCount);
            var request = new PurchaseAttempt(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new Value(Assets.LovelaceUnit, 1000000) }),
                requestedQuantity,
                0);

            Func<Task> asyncTask = async () =>
            {
                var allocated = await _allocator.AllocateTokensForPurchaseAsync(
                    request, allocatedTokens, mintableTokens, sale);
            };

            await asyncTask.Should().ThrowAsync<CannotAllocateMoreThanSaleReleaseException>();
        }

        [Theory]
        [InlineData(0, 1, 1, 1)]
        [InlineData(1, 49, 2, 50)]
        [InlineData(5, 145, 8, 150)]
        public async Task Throws_CannotAllocateMoreThanMintableException_When_No_Mintable_Tokens_Are_Left(
            int saleMintableCount,
            int saleAllocatedCount,
            int requestedQuantity,
            int saleReleaseQuantity)
        {
            var sale = Generator.GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
            var mintableTokens = Generator.GenerateTokens(saleMintableCount);
            var allocatedTokens = Generator.GenerateTokens(saleAllocatedCount);
            var request = new PurchaseAttempt(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new Value(Assets.LovelaceUnit, 1000000) }),
                requestedQuantity,
                0);

            Func<Task> asyncTask = async () =>
            {
                var allocated = await _allocator.AllocateTokensForPurchaseAsync(
                    request, allocatedTokens, mintableTokens, sale);
            };

            await asyncTask.Should().ThrowAsync<CannotAllocateMoreThanMintableException>();
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
            var sale = Generator.GenerateSale(totalReleaseQuantity: saleReleaseQuantity);
            var allocatedTokens = Generator.GenerateTokens(saleAllocatedCount);
            var mintableTokens = Generator.GenerateTokens(saleMintableCount);
            var request = new PurchaseAttempt(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new Value(Assets.LovelaceUnit, 1000000) }),
                requestedQuantity,
                0);

            Func<Task> asyncTask = async () =>
            {
                var allocated = await _allocator.AllocateTokensForPurchaseAsync(
                    request, allocatedTokens, mintableTokens, sale);
            };

            await asyncTask.Should().ThrowAsync<ArgumentException>();
        }
    }
}
