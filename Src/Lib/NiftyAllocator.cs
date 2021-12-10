using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class NiftyAllocator : INiftyAllocator
{
    private readonly ILogger<NiftyAllocator> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly ISaleContextDataStorage _saleContextStore;
    private readonly Random _random;

    public NiftyAllocator(
        ILogger<NiftyAllocator> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        ISaleContextDataStorage saleContextStore)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _saleContextStore = saleContextStore;
        _random = new Random();
    }

    public async Task<Nifty[]> AllocateNiftiesForPurchaseAsync(
        PurchaseAttempt request,
        SaleContext saleContext,
        CancellationToken ct = default)
    {
        if (request.NiftyQuantityRequested <= 0)
        {
            throw new ArgumentException("Cannot request zero or negative token allocation", nameof(request));
        }
        if (request.NiftyQuantityRequested > saleContext.MintableTokens.Count)
        {
            throw new CannotAllocateMoreThanMintableException(
                $"Could not allocate {request.NiftyQuantityRequested} tokens with {saleContext.MintableTokens.Count} mintable nifties",
                request.Utxo,
                saleContext.Sale.Id,
                request.NiftyQuantityRequested,
                saleContext.MintableTokens.Count);
        }
        if (request.NiftyQuantityRequested + saleContext.AllocatedTokens.Count > saleContext.Sale.TotalReleaseQuantity)
        {
            throw new CannotAllocateMoreThanSaleReleaseException(
                "Cannot allocate tokens beyond sale realease quantity",
                request.Utxo,
                saleContext.Sale.Id,
                saleContext.Sale.TotalReleaseQuantity,
                saleContext.AllocatedTokens.Count,
                request.NiftyQuantityRequested);
        }

        var sw = Stopwatch.StartNew();
        // TODO: threadsafe implementation 
        var purchaseAllocated = new List<Nifty>(request.NiftyQuantityRequested);
        while (purchaseAllocated.Count < request.NiftyQuantityRequested)
        {
            var randomIndex = _random.Next(0, saleContext.MintableTokens.Count);
            var tokenAllocated = saleContext.MintableTokens[randomIndex];
            purchaseAllocated.Add(tokenAllocated);
            saleContext.AllocatedTokens.Add(tokenAllocated);
            saleContext.MintableTokens.RemoveAt(randomIndex);
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
                { "SaleId", saleContext.Sale.Id },
                { "Utxo", request.Utxo.ToString() },
                { "AllocatedCount", purchaseAllocated.Count },
            });
        _logger.LogDebug(EventIds.AllocatorAllocateElapsed, $"{nameof(AllocateNiftiesForPurchaseAsync)} completed with {purchaseAllocated.Count} tokens after {sw.ElapsedMilliseconds}ms");

        await _saleContextStore.AddAllocationAsync(purchaseAllocated, saleContext, ct).ConfigureAwait(false);

        return purchaseAllocated.ToArray();
    }
}
