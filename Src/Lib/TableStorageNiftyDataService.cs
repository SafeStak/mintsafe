using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class TableStorageNiftyDataService : INiftyDataService
    {
        private readonly ILogger<TableStorageNiftyDataService> _logger;
        private readonly MintsafeAppSettings _settings;

        public TableStorageNiftyDataService(
            ILogger<TableStorageNiftyDataService> logger,
            MintsafeAppSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task<CollectionAggregate> GetCollectionAggregateAsync(
            Guid collectionId, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            await Task.Delay(1000).ConfigureAwait(false);
            _logger.LogInformation($"Table storage collection aggregate retrieved after {sw.ElapsedMilliseconds}ms");

            throw new NotImplementedException();
        }
    }
}
