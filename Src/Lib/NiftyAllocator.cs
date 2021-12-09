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

public class NiftyAllocator : INiftyAllocator
{
    private readonly ILogger<NiftyAllocator> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly Random _random;

    public NiftyAllocator(
        ILogger<NiftyAllocator> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _random = new Random();
    }

    public Task<Nifty[]> AllocateNiftiesForPurchaseAsync(
        PurchaseAttempt request,
        IList<Nifty> saleAllocatedNfts,
        IList<Nifty> saleMintableNfts,
        Sale sale,
        CancellationToken ct = default)
    {
        if (request.NiftyQuantityRequested <= 0)
        {
            throw new ArgumentException("Cannot request zero or negative token allocation", nameof(request));
        }
        if (request.NiftyQuantityRequested > saleMintableNfts.Count)
        {
            throw new CannotAllocateMoreThanMintableException(
                $"Could not allocate {request.NiftyQuantityRequested} tokens with {saleMintableNfts.Count} mintable nifties",
                request.Utxo,
                sale.Id,
                request.NiftyQuantityRequested,
                saleMintableNfts.Count);
        }
        if (request.NiftyQuantityRequested + saleAllocatedNfts.Count > sale.TotalReleaseQuantity)
        {
            throw new CannotAllocateMoreThanSaleReleaseException(
                "Cannot allocate tokens beyond sale realease quantity",
                request.Utxo,
                sale.Id,
                sale.TotalReleaseQuantity,
                saleAllocatedNfts.Count,
                request.NiftyQuantityRequested);
        }

        var sw = Stopwatch.StartNew();
        var purchaseAllocated = new List<Nifty>(request.NiftyQuantityRequested);
        while (purchaseAllocated.Count < request.NiftyQuantityRequested)
        {
            var randomIndex = _random.Next(0, saleMintableNfts.Count);
            var tokenAllocated = saleMintableNfts[randomIndex];
            purchaseAllocated.Add(tokenAllocated);
            saleAllocatedNfts.Add(tokenAllocated);
            saleMintableNfts.RemoveAt(randomIndex);
        }
        _instrumentor.TrackDependency(
            EventIds.AllocatorAllocateElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(NiftyAllocator),
            string.Empty,
            nameof(AllocateNiftiesForPurchaseAsync),
            isSuccessful: true,
            customProperties: new Dictionary<string, object>
            {
                { "SaleId", sale.Id },
                { "Utxo", request.Utxo.ToString() },
                { "AllocatedCount", purchaseAllocated.Count },
            });
        _logger.LogDebug(EventIds.AllocatorAllocateElapsed, $"{nameof(AllocateNiftiesForPurchaseAsync)} completed with {purchaseAllocated.Count} tokens after {sw.ElapsedMilliseconds}ms");

        var saleFolder = Path.Combine(_settings.BasePath, sale.Id.ToString()[..8]);
        var allocatedNftIdsPath = Path.Combine(saleFolder, "allocatedNftIds.csv");
        sw.Restart();
        File.AppendAllLines(allocatedNftIdsPath, purchaseAllocated.Select(n => n.Id.ToString()));
        _instrumentor.TrackDependency(
            EventIds.AllocatorUpdateStateElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(NiftyAllocator),
            allocatedNftIdsPath,
            "AppendAllocatedNifties",
            isSuccessful: true);

        return Task.FromResult(purchaseAllocated.ToArray());
    }
}
