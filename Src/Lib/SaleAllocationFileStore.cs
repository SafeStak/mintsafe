using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class SaleAllocationFileStore : ISaleAllocationStore
{
    private static readonly object SaleAllocationOperationLock = new();
    private readonly ILogger<NiftyAllocator> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly Random _random;

    public SaleAllocationFileStore(
        ILogger<NiftyAllocator> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _random = new Random();
    }

    public async Task<SaleContext> GetOrRestoreSaleContextAsync(
        CollectionAggregate collectionAggregate, Guid workerId, CancellationToken ct)
    {
        var activeSale = collectionAggregate.ActiveSales[0];
        var mintableTokens = collectionAggregate.Tokens.Where(t => t.IsMintable).ToList();
        var allocatedNfts = new List<Nifty>();
        var saleFolder = Path.Combine(_settings.BasePath, activeSale.Id.ToString()[..8]);
        var saleUtxosFolder = Path.Combine(saleFolder, "utxos");
        var mintableNftIdsSnapshotPath = Path.Combine(saleFolder, "mintableNftIds.csv");
        var allocatedNftIdsPath = Path.Combine(saleFolder, "allocatedNftIds.csv");
        var sw = new Stopwatch();
        // Brand new sale for worker - generate fresh context and persist it
        if (!Directory.Exists(saleFolder))
        {
            Directory.CreateDirectory(saleFolder);
            Directory.CreateDirectory(saleUtxosFolder);
            sw.Start();
            await File.WriteAllLinesAsync(mintableNftIdsSnapshotPath, mintableTokens.Select(n => n.Id.ToString()), ct).ConfigureAwait(false);
            await File.WriteAllTextAsync(allocatedNftIdsPath, string.Empty, ct).ConfigureAwait(false);
        }
        else // Restore sale context from previous execution
        {
            var allocatedNftIdLines = await File.ReadAllLinesAsync(allocatedNftIdsPath, ct).ConfigureAwait(false);
            var allocatedNftIds = new HashSet<string>(allocatedNftIdLines);
            var revisedMintableNfts = new List<Nifty>();
            foreach (var nft in mintableTokens)
            {
                if (allocatedNftIds.Contains(nft.Id.ToString()))
                    allocatedNfts.Add(nft);
                else
                    revisedMintableNfts.Add(nft);
            }
            mintableTokens = revisedMintableNfts;
        }
        var saleContext = new SaleContext(
            workerId,
            saleFolder,
            saleUtxosFolder,
            activeSale,
            collectionAggregate.Collection,
            mintableTokens,
            allocatedNfts,
            new HashSet<UnspentTransactionOutput>(),
            new HashSet<UnspentTransactionOutput>(),
            new HashSet<UnspentTransactionOutput>(),
            new HashSet<UnspentTransactionOutput>());

        _instrumentor.TrackDependency(
            EventIds.SaleContextGetOrRestoreElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(SaleAllocationFileStore),
            allocatedNftIdsPath,
            nameof(GetOrRestoreSaleContextAsync),
            isSuccessful: true,
            customProperties: new Dictionary<string, object>
            {
                { "WorkerId", saleContext.SaleWorkerId },
                { "SaleId", saleContext.Sale.Id },
                { "CollectionId", saleContext.Collection.Id },
                { "SaleContext.AllocatedTokens", saleContext.AllocatedTokens.Count },
                { "SaleContext.MintableTokens", saleContext.MintableTokens.Count },
                { "SaleContext.RefundedUtxos", saleContext.RefundedUtxos.Count },
                { "SaleContext.SuccessfulUtxos", saleContext.SuccessfulUtxos.Count },
                { "SaleContext.FailedUtxos", saleContext.FailedUtxos.Count },
                { "SaleContext.LockedUtxos", saleContext.LockedUtxos.Count },
            });

        return saleContext;
    }

    public Task<Nifty[]> AllocateNiftiesAsync(
        PurchaseAttempt request, SaleContext context, CancellationToken ct)
    {
        var allocatedNftIdsPath = Path.Combine(context.SalePath, "allocatedNftIds.csv");
        if (!File.Exists(allocatedNftIdsPath))
            throw new FileNotFoundException("SaleContext Allocated File Missing", allocatedNftIdsPath);

        var quantityToAllocate = request.NiftyQuantityRequested;
        var sw = Stopwatch.StartNew();
        lock (SaleAllocationOperationLock)
        {
            // One final check within the lock 
            if (quantityToAllocate + context.AllocatedTokens.Count > context.Sale.TotalReleaseQuantity)
            {
                throw new CannotAllocateMoreThanSaleReleaseException(
                    "Cannot allocate tokens beyond sale realease quantity or sale is sold out",
                    request.Utxo,
                    context.Sale.Id,
                    context.Sale.TotalReleaseQuantity,
                    context.AllocatedTokens.Count,
                    quantityToAllocate);
            }

            var purchaseAllocated = new Nifty[quantityToAllocate];
            var count = 0;
            while (count < quantityToAllocate)
            {
                var randomIndex = _random.Next(0, context.MintableTokens.Count);
                var tokenAllocated = context.MintableTokens[randomIndex];
                purchaseAllocated[count++] = tokenAllocated;
                context.AllocatedTokens.Add(tokenAllocated);
                context.MintableTokens.RemoveAt(randomIndex);
            }
            File.AppendAllLines(allocatedNftIdsPath, purchaseAllocated.Select(n => n.Id.ToString()));
            _instrumentor.TrackDependency(
                EventIds.SaleContextAllocateElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(SaleAllocationFileStore),
                allocatedNftIdsPath,
                nameof(AllocateNiftiesAsync),
                isSuccessful: true,
                customProperties: new Dictionary<string, object>
                {
                    { "WorkerId", context.SaleWorkerId },
                    { "SaleId", context.Sale.Id },
                    { "CollectionId", context.Collection.Id },
                    { "SaleContextStore.AllocatedCount", purchaseAllocated.Length },
                });
            return Task.FromResult(purchaseAllocated);
        }
    }

    public Task ReleaseAllocationAsync(
        IReadOnlyCollection<Nifty> allocated, SaleContext context, CancellationToken ct)
    {
        var allocatedNftIdsPath = Path.Combine(context.SalePath, "allocatedNftIds.csv");
        if (!File.Exists(allocatedNftIdsPath))
            throw new FileNotFoundException("SaleContext Allocated File Missing", allocatedNftIdsPath);

        var releasedCount = 0;
        var sw = Stopwatch.StartNew();
        lock (SaleAllocationOperationLock)
        {
            var allocatedNftIds = new HashSet<string>(File.ReadAllLines(allocatedNftIdsPath));
            foreach (var nifty in allocated)
            {
                var successfulFileRelease = allocatedNftIds.Remove(nifty.Id.ToString());
                if (!successfulFileRelease)
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
            EventIds.SaleContextReleaseElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(SaleAllocationFileStore),
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
