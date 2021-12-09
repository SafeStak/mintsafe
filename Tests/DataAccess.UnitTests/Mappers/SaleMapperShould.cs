using System;
using FluentAssertions;
using Mintsafe.DataAccess.Mappers;
using Mintsafe.DataAccess.Models;
using Xunit;

namespace Mintsafe.DataAccess.UnitTests.Mappers
{
    public class SaleMapperShould
    {
        [Fact]
        public void Map_Dto_Correctly()
        {
            var rowKey = Guid.NewGuid();
            var partitionKey = Guid.NewGuid();
            var now = DateTime.Now;

            var sale = new Sale()
            {
                RowKey = rowKey.ToString(),
                PartitionKey = partitionKey.ToString(),
                IsActive = true,
                Name = "Name",
                Description = "Description",
                LovelacesPerToken = 1,
                SaleAddress = "SaleAddress",
                ProceedsAddress = "ProceedsAddress",
                TotalReleaseQuantity = 5,
                MaxAllowedPurchaseQuantity = 2,
                Start = now,
                End = now.AddDays(1)
            };

            var model = SaleMapper.Map(sale);

            model.Should().NotBeNull();
            model.Id.Should().Be(rowKey.ToString());
            model.IsActive.Should().BeTrue();
            model.Name.Should().Be("Name");
            model.Description.Should().Be("Description");
            model.LovelacesPerToken.Should().Be(1);
            model.SaleAddress.Should().Be("SaleAddress");
            model.ProceedsAddress.Should().Be("ProceedsAddress");
            model.TotalReleaseQuantity.Should().Be(5);
            model.MaxAllowedPurchaseQuantity.Should().Be(2);
            model.Start.Should().Be(now);
            model.End.Should().Be(now.AddDays(1));
        }

        [Fact]
        public void Map_Model_Correctly()
        {
            var rowKey = Guid.NewGuid();
            var partitionKey = Guid.NewGuid();
            var now = DateTime.Now;

            var sale = new Abstractions.Sale(
                rowKey,
                partitionKey,
                true,
                "Name",
                "Description",
                1,
                "SaleAddress",
                "CreatorAddress",
                "ProceedsAddress",
                0.1m,
                5,
                2,
                now,
                now.AddDays(1)
            );

            var model = SaleMapper.Map(sale);

            model.Should().NotBeNull();
            model.RowKey.Should().Be(rowKey.ToString());
            model.PartitionKey.Should().Be(partitionKey.ToString());
            model.IsActive.Should().BeTrue();
            model.Name.Should().Be("Name");
            model.Description.Should().Be("Description");
            model.LovelacesPerToken.Should().Be(1);
            model.SaleAddress.Should().Be("SaleAddress");
            model.ProceedsAddress.Should().Be("ProceedsAddress");
            model.TotalReleaseQuantity.Should().Be(5);
            model.MaxAllowedPurchaseQuantity.Should().Be(2);
            model.Start.Should().Be(now);
            model.End.Should().Be(now.AddDays(1));
        }
    }
}
