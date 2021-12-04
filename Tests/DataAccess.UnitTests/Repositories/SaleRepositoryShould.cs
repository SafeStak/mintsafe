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
        private readonly Mock<TableClient> _saleClientMock;

        private readonly ISaleRepository _saleRepository;

        public SaleRepositoryShould()
        {
            _saleClientMock = new Mock<TableClient>();

            var azureClientFactoryMock = new Mock<IAzureClientFactory<TableClient>>();
            azureClientFactoryMock.Setup(x => x.CreateClient("Sale"))
                .Returns(_saleClientMock.Object);

            _saleRepository = new SaleRepository(azureClientFactoryMock.Object);
        }

        [Fact]
        public async Task Return_Sale_Correctly_When_GetByCollectionId_Is_Called()
        {
            var collectionId = Guid.NewGuid();

            var fixture = new Fixture();
            var sale = fixture.Create<Sale>();

            var page = Page<Sale>.FromValues(new[] { sale }, null, new Mock<Response>().Object);

            _saleClientMock.Setup(x => x.QueryAsync<Sale>(x => x.PartitionKey == collectionId.ToString(),
                    It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncPageable<Sale>.FromPages(new[] { page }));

            var result = await _saleRepository.GetByCollectionIdAsync(collectionId, CancellationToken.None);

            result.Should().NotBeNullOrEmpty();
            result.Count().Should().Be(1);
            result.First().Should().Be(sale);
        }

        [Fact]
        public async Task Call_UpdateEntityAsync_When_UpdateOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var sale = fixture.Create<Sale>();

            await _saleRepository.UpdateOneAsync(sale, CancellationToken.None);

            _saleClientMock.Verify(x => x.UpdateEntityAsync(sale, sale.ETag, TableUpdateMode.Merge, It.IsAny<CancellationToken>()));
        }


        [Fact]
        public async Task Call_AddEntityAsync_When_InsertOneAsync_Is_Called()
        {
            var fixture = new Fixture();
            var sale = fixture.Create<Sale>();

            await _saleRepository.InsertOneAsync(sale, CancellationToken.None);

            _saleClientMock.Verify(x => x.AddEntityAsync(sale, It.IsAny<CancellationToken>()));
        }
    }
}
