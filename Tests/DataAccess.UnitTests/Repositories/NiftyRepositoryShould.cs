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
        private readonly Mock<TableClient> _niftyClientMock;
        private readonly INiftyRepository _niftyRepository;

        public NiftyRepositoryShould()
        {
            _niftyClientMock = new Mock<TableClient>();

            var azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            azureClientFactoryMock.Setup(x => x.CreateClient("Nifty"))
                .Returns(_niftyClientMock.Object);

            _niftyRepository = new NiftyRepository(azureClientFactoryMock.Object);
        }

        [Fact]
        public async Task Return_Nifty_Correctly_When_GetByCollectionId_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString);
            var Nifty = fixture.Create();

            var page = Page<Nifty>.FromValues(new[] { Nifty }, null, new Mock<Response>().Object);

            _niftyClientMock.Setup(x => x.QueryAsync<Nifty>(x => x.PartitionKey == collectionId.ToString(),
                    It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncPageable<Nifty>.FromPages(new[] { page }));

            var result = await _niftyRepository.GetByCollectionIdAsync(collectionId, CancellationToken.None);

            result.Should().NotBeNullOrEmpty();
            result.Count().Should().Be(1);
            result.First().Should().Be(Nifty);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {

            var fixture = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString);
            var nifty = fixture.Create();

            await _niftyRepository.UpdateOneAsync(nifty, CancellationToken.None);

            _niftyClientMock.Verify(x => x.UpdateEntityAsync(nifty, nifty.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync_When_InsertOneAsync_Is_Called()
        {

            var fixture = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString);
            var nifty = fixture.Create();

            await _niftyRepository.InsertOneAsync(nifty, CancellationToken.None);

            _niftyClientMock.Verify(x => x.AddEntityAsync(nifty, It.IsAny<CancellationToken>()));
        }
    }
}
