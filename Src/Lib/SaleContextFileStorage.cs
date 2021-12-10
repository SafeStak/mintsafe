using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    
    public class SaleContextFileStorage : ISaleContextDataStorage
    {
        private static readonly object SaleContextOperationLock = new();
        private readonly ILogger<NiftyAllocator> _logger;
        private readonly IInstrumentor _instrumentor;
        private readonly MintsafeAppSettings _settings;

        public SaleContextFileStorage(
            ILogger<NiftyAllocator> logger,
            IInstrumentor instrumentor,
            MintsafeAppSettings settings)
        {
            _logger = logger;
            _instrumentor = instrumentor;
            _settings = settings;
        }

        public Task AddAllocationAsync(IEnumerable<Nifty> allocated, SaleContext context, CancellationToken ct)
        {
            var allocatedNftIdsPath = Path.Combine(context.SalePath, "allocatedNftIds.csv");
            if (!File.Exists(allocatedNftIdsPath))
                throw new FileNotFoundException("SaleContext Allocated File Missing", allocatedNftIdsPath);

            var sw = Stopwatch.StartNew();
            lock (SaleContextOperationLock)
            {
                File.AppendAllLines(allocatedNftIdsPath, allocated.Select(n => n.Id.ToString()));
            }
            _instrumentor.TrackDependency(
                EventIds.AllocatorUpdateStateElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(SaleContextFileStorage),
                allocatedNftIdsPath,
                nameof(AddAllocationAsync),
                isSuccessful: true);
            return Task.CompletedTask;
        }

        public Task ReleaseAllocationAsync(IEnumerable<Nifty> allocated, SaleContext context, CancellationToken ct)
        {
            var allocatedNftIdsPath = Path.Combine(context.SalePath, "allocatedNftIds.csv");
            if (!File.Exists(allocatedNftIdsPath))
                throw new FileNotFoundException("SaleContext Allocated File Missing", allocatedNftIdsPath);

            var releasedCount = 0;
            var sw = Stopwatch.StartNew();
            lock (SaleContextOperationLock)
            {
                var allocatedNftIds = new HashSet<string>(File.ReadAllLines(allocatedNftIdsPath));
                foreach (var nifty in allocated)
                {
                    var successfulFileRelease = allocatedNftIds.Remove(nifty.Id.ToString());
                    if (successfulFileRelease)
                    {
                        _logger.LogWarning(EventIds.GeneralWarning, $"Cannot fully release allocation for ID {nifty.Id} - not found in file");
                    }
                    var niftyToRelease = context.AllocatedTokens.FirstOrDefault(n => n.Id == nifty.Id);
                    if (niftyToRelease == null)
                    {
                        _logger.LogWarning(EventIds.GeneralWarning, $"Cannot fully release allocation for ID {nifty.Id} - not found in context");
                        continue;
                    }
                    context.MintableTokens.Add(niftyToRelease);
                    context.AllocatedTokens.Remove(niftyToRelease);
                    if (successfulFileRelease && niftyToRelease != null)
                    {
                        _logger.LogDebug(EventIds.GeneralDebug, $"Fully released allocation for {nifty.Id}");
                        releasedCount++;
                    }
                }
                File.WriteAllLines(allocatedNftIdsPath, allocatedNftIds);
            }
            _instrumentor.TrackDependency(
                EventIds.AllocatorUpdateStateElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(SaleContextFileStorage),
                allocatedNftIdsPath,
                nameof(ReleaseAllocationAsync),
                isSuccessful: true,
                customProperties: new Dictionary<string, object>
                {
                    { "WorkerId", context.SaleWorkerId },
                    { "SaleId", context.Sale.Id },
                    { "CollectionId", context.Collection.Id },
                    { "SaleContextStore.ReleasedCount", releasedCount },
                });
            return Task.CompletedTask;
        }
    }
}
