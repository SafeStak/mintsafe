using System;
using System.Collections.Generic;
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
    public class NiftyCollectionRepositoryShould
    {
        private readonly Mock<IAzureClientFactory<TableClient>> _azureClientFactoryMock;
        private readonly Mock<TableClient> _niftyCollectionClientMock;

        public NiftyCollectionRepositoryShould()
        {
            _azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            _niftyCollectionClientMock = new Mock<TableClient>();
        }

        [Fact]
        public async Task Return_NiftyCollection_Correctly_When_GetById_Is_Called()
        {
            var id = Guid.NewGuid();

            var fixture = new Fixture();
            var niftyCollection = fixture.Create<NiftyCollection>();

            var page = Page<NiftyCollection>.FromValues(new[] {niftyCollection}, null, new Mock<Response>().Object);

            _niftyCollectionClientMock.Setup(x => x.QueryAsync<NiftyCollection>(x => x.RowKey == id.ToString(),
                    It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncPageable<NiftyCollection>.FromPages(new[] { page }));

            _azureClientFactoryMock.Setup(x => x.CreateClient("NiftyCollection"))
                .Returns(_niftyCollectionClientMock.Object);

            var repo = new NiftyCollectionRepository(_azureClientFactoryMock.Object);
            var result = await repo.GetByIdAsync(id, CancellationToken.None);

            result.Should().Be(niftyCollection);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var niftyCollection = fixture.Create<NiftyCollection>();

            _azureClientFactoryMock.Setup(x => x.CreateClient("NiftyCollection"))
                .Returns(_niftyCollectionClientMock.Object);

            var repo = new NiftyCollectionRepository(_azureClientFactoryMock.Object);
            await repo.UpdateOneAsync(niftyCollection, CancellationToken.None);

            _niftyCollectionClientMock.Verify(x => x.UpdateEntityAsync(niftyCollection, niftyCollection.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync_When_InsertOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var niftyCollection = fixture.Create<NiftyCollection>();

            _azureClientFactoryMock.Setup(x => x.CreateClient("NiftyCollection"))
                .Returns(_niftyCollectionClientMock.Object);

            var repo = new NiftyCollectionRepository(_azureClientFactoryMock.Object);
            await repo.InsertOneAsync(niftyCollection, CancellationToken.None);

            _niftyCollectionClientMock.Verify(x => x.AddEntityAsync(niftyCollection, It.IsAny<CancellationToken>()));
        }
    }
}
