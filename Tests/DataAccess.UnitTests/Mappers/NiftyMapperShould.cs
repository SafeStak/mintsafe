using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Mappers;
using Xunit;
using Nifty = Mintsafe.DataAccess.Models.Nifty;
using NiftyFile = Mintsafe.DataAccess.Models.NiftyFile;

namespace Mintsafe.DataAccess.UnitTests.Mappers
{
    public class NiftyMapperShould
    {
        [Fact]
        public void Map_Dto_Correctly()
        {
            var now = DateTime.Now;
            var rowKey = Guid.NewGuid();
            var partitionKey = Guid.NewGuid();
            var niftyId = Guid.NewGuid();

            var nifty = new Nifty()
            {
                RowKey = rowKey.ToString(),
                PartitionKey = partitionKey.ToString(),
                IsMintable = true,
                AssetName = "Asset Name",
                Name = "Name",
                Description = "Description",
                Creators = new[] { "Me", "You" },
                Image = "Image",
                MediaType = "jpeg",
                CreatedAt = now,
                Version = "Version",
                RoyaltyPortion = 1.0,
                RoyaltyAddress = "RoyaltyAddress",
                Attributes = new List<KeyValuePair<string, string>>() { new("key", "value") }
            };

            var niftyFiles = new NiftyFile[]
            {
                new()
                {
                    RowKey = rowKey.ToString(),
                    PartitionKey = partitionKey.ToString(),
                    NiftyId = niftyId.ToString(),
                    Name = "Name",
                    MediaType = "jpeg",
                    Url = "test.com",
                    FileHash = "hash"
                }
            };

            var model = NiftyMapper.Map(nifty, niftyFiles);

            model.Should().NotBeNull();

            model.Id.Should().Be(rowKey);
            model.CollectionId.Should().Be(partitionKey);
            model.IsMintable.Should().BeTrue();
            model.AssetName.Should().Be("Asset Name");
            model.Name.Should().Be("Name");
            model.Description.Should().Be("Description");
            model.Creators.Should().BeEquivalentTo("Me", "You");
            model.Image.Should().Be("Image");
            model.MediaType.Should().Be("jpeg");
            model.CreatedAt.Should().Be(now);
            model.Royalty.Address.Should().Be("RoyaltyAddress");
            model.Royalty.PortionOfSale.Should().Be(1.0);
            model.Version.Should().Be("Version");
            model.Attributes.Should().BeEquivalentTo(new List<KeyValuePair<string, string>>() { new("key", "value") });

            model.Files.First().Should().NotBeNull();
            model.Files.First().Id.Should().Be(rowKey.ToString());
            model.Files.First().NiftyId.Should().Be(niftyId);
            model.Files.First().Name.Should().Be("Name");
            model.Files.First().MediaType.Should().Be("jpeg");
            model.Files.First().Url.Should().Be("test.com");
            model.Files.First().FileHash.Should().Be("hash");
        }

        [Fact]
        public void Map_Model_Correctly()
        {
            var rowKey = Guid.NewGuid();
            var niftyId = Guid.NewGuid();
            var collectionId = Guid.NewGuid();
            var now = DateTime.Now;

            var nifty = new Abstractions.Nifty(
                rowKey,
                collectionId,
                true,
                "Asset Name",
                "Name",
                "Description",
                new[] { "Me", "You" },
                "Image",
                "jpeg",
                new[]
                    {
                        new Abstractions.NiftyFile(
                            rowKey,
                            niftyId,
                            "jpeg",
                            "test.com",
                            "hash"
                            )
                    },
                now,
                new Royalty(1.0, "RoyaltyAddress"),
                "Version",
                new KeyValuePair<string, string>[] { new("key", "value") }
            );

            var model = NiftyMapper.Map(nifty);

            model.Should().NotBeNull();
            model.RowKey.Should().Be(rowKey.ToString());
            model.PartitionKey.Should().Be(collectionId.ToString());
            model.IsMintable.Should().BeTrue();
            model.AssetName.Should().Be("Asset Name");
            model.Name.Should().Be("Name");
            model.Description.Should().Be("Description");
            model.Creators.Should().BeEquivalentTo("Me", "You");
            model.Image.Should().Be("Image");
            model.MediaType.Should().Be("jpeg");
            model.CreatedAt.Should().Be(now);
            model.Version.Should().Be("Version");
            model.RoyaltyAddress.Should().Be("RoyaltyAddress");
            model.RoyaltyPortion.Should().Be(1.0);
            model.Attributes.Should().BeEquivalentTo(new List<KeyValuePair<string, string>>() { new("key", "value") });
        }
    }
}
