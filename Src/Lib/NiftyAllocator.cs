using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

// TODO: Replace this with ISaleContextDataStorage
public class NiftyAllocator : INiftyAllocator
{
    private readonly ILogger<NiftyAllocator> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly ISaleAllocationStore _saleContextStore;

    public NiftyAllocator(
        ILogger<NiftyAllocator> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        ISaleAllocationStore saleContextStore)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _saleContextStore = saleContextStore;
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
        if (request.NiftyQuantityRequested + saleContext.AllocatedTokens.Count > saleContext.Sale.TotalReleaseQuantity)
        {
            throw new CannotAllocateMoreThanSaleReleaseException(
                "Cannot allocate tokens beyond sale realease quantity or sale is sold out",
                request.Utxo,
                saleContext.Sale.Id,
                saleContext.Sale.TotalReleaseQuantity,
                saleContext.AllocatedTokens.Count,
                request.NiftyQuantityRequested);
        }

        var purchaseAllocated = await _saleContextStore.AllocateNiftiesAsync(request, saleContext, ct).ConfigureAwait(false);

        return purchaseAllocated;
    }
}
