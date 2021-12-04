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
        private readonly Mock<TableClient> _niftyFileClientMock;

        private readonly INiftyFileRepository _niftyFileRepository;

        public NiftyFileRepositoryShould()
        {
            _niftyFileClientMock = new Mock<TableClient>();

            var azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            azureClientFactoryMock.Setup(x => x.CreateClient("NiftyFile"))
                .Returns(_niftyFileClientMock.Object);

            _niftyFileRepository = new NiftyFileRepository(azureClientFactoryMock.Object);
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

            var result = await _niftyFileRepository.GetByCollectionIdAsync(collectionId, CancellationToken.None);

            result.Should().NotBeNullOrEmpty();
            result.Count().Should().Be(1);
            result.First().Should().Be(niftyFile);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var niftyFile = fixture.Create<NiftyFile>();

            await _niftyFileRepository.UpdateOneAsync(niftyFile, CancellationToken.None);

            _niftyFileClientMock.Verify(x => x.UpdateEntityAsync(niftyFile, niftyFile.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync_When_InsertOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var niftyFile = fixture.Create<NiftyFile>();
            
            await _niftyFileRepository.InsertOneAsync(niftyFile, CancellationToken.None);

            _niftyFileClientMock.Verify(x => x.AddEntityAsync(niftyFile, It.IsAny<CancellationToken>()));
        }
    }
}
