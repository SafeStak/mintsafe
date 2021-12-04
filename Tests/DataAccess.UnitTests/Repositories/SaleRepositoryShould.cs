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
    public class SaleRepositoryShould
    {
        private readonly Mock<IAzureClientFactory<TableClient>> _azureClientFactoryMock;
        private readonly Mock<TableClient> _SaleClientMock;

        public SaleRepositoryShould()
        {
            _azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            _SaleClientMock = new Mock<TableClient>();
        }

        [Fact]
        public async Task Return_Sale_Correctly_When_GetByCollectionId_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture();
            var sale = fixture.Create<Sale>();

            var page = Page<Sale>.FromValues(new[] { sale }, null, new Mock<Response>().Object);

            _SaleClientMock.Setup(x => x.QueryAsync<Sale>(x => x.PartitionKey == collectionId.ToString(),
                    It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncPageable<Sale>.FromPages(new[] { page }));

            _azureClientFactoryMock.Setup(x => x.CreateClient("Sale"))
                .Returns(_SaleClientMock.Object);

            var repo = new SaleRepository(_azureClientFactoryMock.Object);
            var result = await repo.GetByCollectionIdAsync(collectionId, CancellationToken.None);

            result.Should().NotBeNullOrEmpty();
            result.Count().Should().Be(1);
            result.First().Should().Be(sale);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var sale = fixture.Create<Sale>();

            _azureClientFactoryMock.Setup(x => x.CreateClient("Sale"))
                .Returns(_SaleClientMock.Object);

            var repo = new SaleRepository(_azureClientFactoryMock.Object);
            await repo.UpdateOneAsync(sale, CancellationToken.None);

            _SaleClientMock.Verify(x => x.UpdateEntityAsync(sale, sale.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync_And_Set_RowKey_When_InsertOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var sale = fixture.Create<Sale>();

            sale.RowKey = null;

            _azureClientFactoryMock.Setup(x => x.CreateClient("Sale"))
                .Returns(_SaleClientMock.Object);

            var repo = new SaleRepository(_azureClientFactoryMock.Object);
            await repo.InsertOneAsync(sale, CancellationToken.None);

            sale.RowKey.Should().NotBeNull();

            _SaleClientMock.Verify(x => x.AddEntityAsync(sale, It.IsAny<CancellationToken>()));
        }
    }
}
