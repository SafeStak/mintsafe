using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NiftyLaunchpad.Lib.UnitTests
{
    public class TokenAllocatorShould
    {
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(10, 1, 1)]
        [InlineData(10, 10, 10)]
        public async Task Allocate_Right_Amount_Of_Tokens_Given_Sufficient_Mintable_Tokens_When_Purchase_Request_Is_Valid(
            int mintableTokenCount, int requestedQuantity, int expectedAllocatedQuantity)
        {
            var mintableTokens = GenerateTokens(mintableTokenCount);
            var _allocator = new TokenManager(mintableTokens);
            var request = new NiftySalePurchaseRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new UtxoValue("lovelace", 1000000) }),
                requestedQuantity,
                0);

            var allocated = await _allocator.AllocateTokensAsync(request);

            allocated.Length.Should().Be(expectedAllocatedQuantity);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(5, 8)]
        public void Throws_AllMintableTokensForSaleAllocated_When_No_Mintable_Tokens_Are_Left(
            int mintableTokenCount, int requestedQuantity)
        {
            var mintableTokens = GenerateTokens(mintableTokenCount);
            var _allocator = new TokenManager(mintableTokens);
            var request = new NiftySalePurchaseRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new UtxoValue("lovelace", 1000000) }),
                requestedQuantity,
                0);

            Func<Task> asyncTask = async () =>
            {
                var allocated = await _allocator.AllocateTokensAsync(request);
            };

            asyncTask.Should().ThrowAsync<SaleInactiveException>();
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, -1)]
        [InlineData(5, -10)]
        public void Throws_ArgumentException_When_Requesting_Zero_Or_Negative_Token_Quantity(
            int mintableTokenCount, int requestedQuantity)
        {
            var mintableTokens = GenerateTokens(mintableTokenCount);
            var _allocator = new TokenManager(mintableTokens);
            var request = new NiftySalePurchaseRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Utxo("", 0, new[] { new UtxoValue("lovelace", 1000000) }),
                requestedQuantity,
                0);

            Func<Task> asyncTask = async () =>
            {
                var allocated = await _allocator.AllocateTokensAsync(request);
            };

            asyncTask.Should().ThrowAsync<ArgumentException>();
        }

        private static List<Nifty> GenerateTokens(int mintableTokenCount)
        {
            return Enumerable.Range(0, mintableTokenCount)
                .Select(i => new Nifty(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    true,
                    $"Token{i}",
                    $"Token {i}",
                    $"Token {i} Description",
                    new[] { "NiftyLaunchpad.net" },
                    $"ipfs://{i}",
                    "image/png",
                    Array.Empty<NiftyFile>(),
                    DateTime.UtcNow,
                    new Royalty(0, string.Empty),
                    "1.0",
                    new Dictionary<string, string>()))
                .ToList();
        }
    }
}
