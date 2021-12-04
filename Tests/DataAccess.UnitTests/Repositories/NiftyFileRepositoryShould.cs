using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Azure;
using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess.Models;
using Mintsafe.DataAccess.Repositories;
using Moq;
using Xunit;

namespace Mintsafe.DataAccess.UnitTests.Repositories
{
    public class NiftyFileRepositoryShould
    {
        private readonly Mock<IAzureClientFactory<TableClient>> _azureClientFactoryMock;
        private readonly Mock<TableClient> _niftyFileClientMock;

        public NiftyFileRepositoryShould()
        {
            _azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            _niftyFileClientMock = new Mock<TableClient>();
        }

        [Fact]
        public async Task Return_NiftyFile_Correctly_When_GetByCollectionId_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture();
            var niftyFile = fixture.Create<NiftyFile>();

            var page = Page<NiftyFile>.FromValues(new[] { niftyFile }, null, new Mock<Response>().Object);

            _niftyFileClientMock.Setup(x => x.QueryAsync<NiftyFile>(x => x.PartitionKey == collectionId.ToString(),
                    It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncPageable<NiftyFile>.FromPages(new[] { page }));

            _azureClientFactoryMock.Setup(x => x.CreateClient("NiftyFile"))
                .Returns(_niftyFileClientMock.Object);

            var repo = new NiftyFileRepository(_azureClientFactoryMock.Object);
            var result = await repo.GetByCollectionId(collectionId, CancellationToken.None);

            result.Should().NotBeNullOrEmpty();
            result.Count().Should().Be(1);
            result.First().Should().Be(niftyFile);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture();
            var niftyFile = fixture.Create<NiftyFile>();
            niftyFile.PartitionKey = null;

            _azureClientFactoryMock.Setup(x => x.CreateClient("NiftyFile"))
                .Returns(_niftyFileClientMock.Object);

            var repo = new NiftyFileRepository(_azureClientFactoryMock.Object);
            await repo.UpdateOneAsync(collectionId, niftyFile, CancellationToken.None);

            niftyFile.PartitionKey.Should().Be(collectionId.ToString());
            _niftyFileClientMock.Verify(x => x.UpdateEntityAsync(niftyFile, niftyFile.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync_And_Set_RowKey_When_InsertOneAsync_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture();
            var niftyFile = fixture.Create<NiftyFile>();

            niftyFile.RowKey = null;

            _azureClientFactoryMock.Setup(x => x.CreateClient("NiftyFile"))
                .Returns(_niftyFileClientMock.Object);

            var repo = new NiftyFileRepository(_azureClientFactoryMock.Object);
            await repo.InsertOneAsync(collectionId, niftyFile, CancellationToken.None);

            niftyFile.RowKey.Should().NotBeNull();

            _niftyFileClientMock.Verify(x => x.AddEntityAsync(niftyFile, It.IsAny<CancellationToken>()));
        }
    }
}
