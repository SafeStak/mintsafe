using System;
using FluentAssertions;
using Mintsafe.DataAccess.Mappers;
using Mintsafe.DataAccess.Models;
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

            var niftyCollection = new NiftyCollection()
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
                SlotExpiry = 5
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
                5
            );

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
        }
    }
}
