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
    public class NiftyRepositoryShould
    {
        private readonly Mock<IAzureClientFactory<TableClient>> _azureClientFactoryMock;
        private readonly Mock<TableClient> _NiftyClientMock;

        public NiftyRepositoryShould()
        {
            _azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            _NiftyClientMock = new Mock<TableClient>();
        }

        [Fact]
        public async Task Return_Nifty_Correctly_When_GetByCollectionId_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString);
            var Nifty = fixture.Create();

            var page = Page<Nifty>.FromValues(new[] { Nifty }, null, new Mock<Response>().Object);

            _NiftyClientMock.Setup(x => x.QueryAsync<Nifty>(x => x.PartitionKey == collectionId.ToString(),
                    It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncPageable<Nifty>.FromPages(new[] { page }));

            _azureClientFactoryMock.Setup(x => x.CreateClient("Nifty"))
                .Returns(_NiftyClientMock.Object);

            var repo = new NiftyRepository(_azureClientFactoryMock.Object);
            var result = await repo.GetByCollectionIdAsync(collectionId, CancellationToken.None);

            result.Should().NotBeNullOrEmpty();
            result.Count().Should().Be(1);
            result.First().Should().Be(Nifty);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {

            var fixture = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString);
            var Nifty = fixture.Create();

            _azureClientFactoryMock.Setup(x => x.CreateClient("Nifty"))
                .Returns(_NiftyClientMock.Object);

            var repo = new NiftyRepository(_azureClientFactoryMock.Object);
            await repo.UpdateOneAsync(Nifty, CancellationToken.None);

            _NiftyClientMock.Verify(x => x.UpdateEntityAsync(Nifty, Nifty.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync__When_InsertOneAsync_Is_Called()
        {

            var fixture = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString);
            var Nifty = fixture.Create();

            _azureClientFactoryMock.Setup(x => x.CreateClient("Nifty"))
                .Returns(_NiftyClientMock.Object);

            var repo = new NiftyRepository(_azureClientFactoryMock.Object);
            await repo.InsertOneAsync(Nifty, CancellationToken.None);

            _NiftyClientMock.Verify(x => x.AddEntityAsync(Nifty, It.IsAny<CancellationToken>()));
        }
    }
}
