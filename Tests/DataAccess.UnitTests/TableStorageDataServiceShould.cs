using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Composers;
using Mintsafe.DataAccess.Repositories;
using Mintsafe.DataAccess.Supporting;
using Moq;
using Xunit;
using Nifty = Mintsafe.DataAccess.Models.Nifty;
using NiftyCollection = Mintsafe.DataAccess.Models.NiftyCollection;
using NiftyFile = Mintsafe.DataAccess.Models.NiftyFile;
using Sale = Mintsafe.DataAccess.Models.Sale;

namespace Mintsafe.DataAccess.UnitTests
{
    public class TableStorageDataServiceShould
    {
        private readonly Mock<INiftyCollectionRepository> _niftyCollectionRepositoryMock;
        private readonly Mock<INiftyRepository> _niftyRepositoryMock;
        private readonly Mock<ISaleRepository> _saleRepositoryMock;
        private readonly Mock<INiftyFileRepository> _niftyFileRepositoryMock;

        private readonly Mock<IAggregateComposer> _collectionAggregateComposerMock;

        private readonly Mock<ILogger<TableStorageDataService>> _loggerMock;

        private readonly TableStorageDataService _tableStorageDataService;

        public TableStorageDataServiceShould()
        {
            _niftyCollectionRepositoryMock = new Mock<INiftyCollectionRepository>();
            _niftyRepositoryMock = new Mock<INiftyRepository>();
            _saleRepositoryMock = new Mock<ISaleRepository>();
            _niftyFileRepositoryMock = new Mock<INiftyFileRepository>();
            _collectionAggregateComposerMock = new Mock<IAggregateComposer>();
            _loggerMock = new Mock<ILogger<TableStorageDataService>>();

            
            _tableStorageDataService = new TableStorageDataService(_niftyCollectionRepositoryMock.Object,
                _saleRepositoryMock.Object, _niftyRepositoryMock.Object, _niftyFileRepositoryMock.Object, _collectionAggregateComposerMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Return_Correctly()
        {
            var collectionId = Guid.NewGuid();

            var niftyCollection = new Fixture().Build<NiftyCollection>().Create();
            _niftyCollectionRepositoryMock.Setup(x => x.GetByIdAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(niftyCollection);

            var nifty = new Fixture().Build<Nifty>().Without(x => x.AttributesAsString).Without(x => x.CreatorsAsString).Create();
            _niftyRepositoryMock.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new []{nifty});

            var sale = new Fixture().Build<Sale>().Create();
            _saleRepositoryMock.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {sale});

            var niftyFile = new Fixture().Build<NiftyFile>().Create();
            _niftyFileRepositoryMock.Setup(x => x.GetByCollectionIdAsync(collectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] {niftyFile});

            var collectionAggregate = new Fixture().Build<ProjectAggregate>().Create();
            _collectionAggregateComposerMock.Setup(x => x.Build(niftyCollection, new[] {nifty}, new[] {sale}, new[] {niftyFile})).Returns(collectionAggregate);

            var result = await _tableStorageDataService.GetCollectionAggregateAsync(collectionId);
            result.Should().Be(collectionAggregate);

            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<FormattedLogValues>(v => v.ToString().Contains("Finished getting all entities for collectionId")),
                null,
                It.IsAny<Func<object, Exception, string>>()
                )
            );
        }

        [Fact]
        public async Task Log_And_Throw_On_NiftyCollection_Exception()
        {
            var exception = new Exception();
            _niftyCollectionRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task<ProjectAggregate>> act = async () => await _tableStorageDataService.GetCollectionAggregateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();

            _loggerMock.Verify(x => x.Log(
                    LogLevel.Error,
                    Constants.EventIds.FailedToRetrieve,
                    It.Is<FormattedLogValues>(v => v.ToString().Contains("Failed to retrieve entities from table storage for collectionId")),
                    exception,
                    It.IsAny<Func<object, Exception, string>>()
                )
            );
        }

        [Fact]
        public async Task Log_And_Throw_On_Nifty_Exception()
        {
            var exception = new Exception();
            _niftyRepositoryMock.Setup(x => x.GetByCollectionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task<ProjectAggregate>> act = async () => await _tableStorageDataService.GetCollectionAggregateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();

            _loggerMock.Verify(x => x.Log(
                    LogLevel.Error,
                    Constants.EventIds.FailedToRetrieve,
                    It.Is<FormattedLogValues>(v => v.ToString().Contains("Failed to retrieve entities from table storage for collectionId")),
                    exception,
                    It.IsAny<Func<object, Exception, string>>()
                )
            );
        }

        [Fact]
        public async Task Log_And_Throw_On_NiftyFile_Exception()
        {
            var exception = new Exception();
            _niftyFileRepositoryMock.Setup(x => x.GetByCollectionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task<ProjectAggregate>> act = async () => await _tableStorageDataService.GetCollectionAggregateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();

            _loggerMock.Verify(x => x.Log(
                    LogLevel.Error,
                    Constants.EventIds.FailedToRetrieve,
                    It.Is<FormattedLogValues>(v => v.ToString().Contains("Failed to retrieve entities from table storage for collectionId")),
                    exception,
                    It.IsAny<Func<object, Exception, string>>()
                )
            );
        }

        [Fact]
        public async Task Log_And_Throw_On_Sales_Exception()
        {
            var exception = new Exception();
            _saleRepositoryMock.Setup(x => x.GetByCollectionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            Func<Task<ProjectAggregate>> act = async () => await _tableStorageDataService.GetCollectionAggregateAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<Exception>();

            _loggerMock.Verify(x => x.Log(
                    LogLevel.Error,
                    Constants.EventIds.FailedToRetrieve,
                    It.Is<FormattedLogValues>(v => v.ToString().Contains("Failed to retrieve entities from table storage for collectionId")),
                    exception,
                    It.IsAny<Func<object, Exception, string>>()
                )
            );
        }
    }
}