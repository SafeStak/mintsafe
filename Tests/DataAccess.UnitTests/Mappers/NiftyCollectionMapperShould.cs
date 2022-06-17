using System;
using FluentAssertions;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Mappers;
using Xunit;

namespace Mintsafe.DataAccess.UnitTests.Mappers
{
    public class NiftyCollectionMapperShould
    {
        [Fact]
        public void Map_Dto_Correctly()
        {
            var now = DateTime.UtcNow;
            var rowKey = Guid.NewGuid();

            var niftyCollection = new DataAccess.Models.NiftyCollection()
            {
                RowKey = rowKey.ToString(),
                PartitionKey = "1",
                PolicyId = "3",
                Name = "Name",
                Description = "Description",
                IsActive = true,
                BrandImage = "img",
                Publishers = new[] { "Me", "You" },
                CreatedAt = now,
                LockedAt = now.AddDays(-1),
                SlotExpiry = 5,
                RoyaltyPortion = 0.1,
                RoyaltyAddress = "addr1x"
            };

            var model = NiftyCollectionMapper.Map(niftyCollection);

            model.Should().NotBeNull();
            model.Id.Should().Be(rowKey.ToString());
            model.PolicyId.Should().Be("3");
            model.Name.Should().Be("Name");
            model.Description.Should().Be("Description");
            model.IsActive.Should().BeTrue();
            model.BrandImage.Should().Be("img");
            model.Publishers.Should().BeEquivalentTo("Me", "You");
            model.CreatedAt.Should().Be(now);
            model.LockedAt.Should().Be(now.AddDays(-1));
            model.SlotExpiry.Should().Be(5);
            model.Royalty.PortionOfSale.Should().Be(0.1);
            model.Royalty.Address.Should().Be("addr1x");
        }

        [Fact]
        public void Map_Model_Correctly()
        {
            var now = DateTime.UtcNow;
            var rowKey = Guid.NewGuid();

            var niftyCollection = new Abstractions.NiftyCollection(
                rowKey,
                "3",
               "Name",
                "Description",
                true,
                "img",
                new[] { "Me", "You" },
                 now,
                now.AddDays(-1),
                5,
                new Royalty(0, string.Empty));

            var model = NiftyCollectionMapper.Map(niftyCollection);

            model.Should().NotBeNull();
            model.RowKey.Should().Be(rowKey.ToString());
            model.PartitionKey.Should().Be("3");
            model.PolicyId.Should().Be("3");
            model.Name.Should().Be("Name");
            model.Description.Should().Be("Description");
            model.IsActive.Should().BeTrue();
            model.BrandImage.Should().Be("img");
            model.Publishers.Should().BeEquivalentTo("Me", "You");
            model.CreatedAt.Should().Be(now);
            model.LockedAt.Should().Be(now.AddDays(-1));
            model.SlotExpiry.Should().Be(5);
            model.RoyaltyAddress.Should().BeEmpty();
            model.RoyaltyPortion.Should().Be(0);
        }
    }
}
