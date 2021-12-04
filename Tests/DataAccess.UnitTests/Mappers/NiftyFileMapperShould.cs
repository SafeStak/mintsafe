using System;
using FluentAssertions;
using Mintsafe.DataAccess.Mappers;
using Mintsafe.DataAccess.Models;
using Xunit;

namespace Mintsafe.DataAccess.UnitTests.Mappers
{
    public class NiftyFileMapperShould
    {
        [Fact]
        public void Map_Dto_Correctly()
        {
            var rowKey = Guid.NewGuid();
            var niftyId = Guid.NewGuid();

            var niftyFile = new NiftyFile()
            {
                RowKey = rowKey.ToString(),
                PartitionKey = "1",
                NiftyId = niftyId.ToString(),
                Name = "Name",
                MediaType = "jpeg",
                Url = "test.com",
                FileHash = "hash"
            };

            var model = NiftyFileMapper.Map(niftyFile);

            model.Should().NotBeNull();
            model.Id.Should().Be(rowKey.ToString());
            model.NiftyId.Should().Be(niftyId);
            model.Name.Should().Be("Name");
            model.MediaType.Should().Be("jpeg");
            model.Url.Should().Be("test.com");
            model.FileHash.Should().Be("hash");
        }

        [Fact]
        public void Map_Model_Correctly()
        {
            var rowKey = Guid.NewGuid();
            var niftyId = Guid.NewGuid();
            var collectionId = Guid.NewGuid();

            var niftyFile = new Abstractions.NiftyFile(
                rowKey,
                niftyId,
                "Name",
                "jpeg",
                "test.com",
                "hash"
            );

            var model = NiftyFileMapper.Map(collectionId, niftyFile);

            model.Should().NotBeNull();
            model.RowKey.Should().Be(rowKey.ToString());
            model.PartitionKey.Should().Be(collectionId.ToString());
            model.NiftyId.Should().Be(niftyId.ToString());
            model.Name.Should().Be("Name");
            model.MediaType.Should().Be("jpeg");
            model.Url.Should().Be("test.com");
            model.FileHash.Should().Be("hash");
        }
    }
}
