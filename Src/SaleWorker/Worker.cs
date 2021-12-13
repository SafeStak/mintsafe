using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.SaleWorker;

public class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Worker> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly INiftyDataService _niftyDataService;
    private readonly ISaleAllocationStore _saleContextDataStorage;
    private readonly IUtxoRetriever _utxoRetriever;
    private readonly ISaleUtxoHandler _saleUtxoHandler;
    private readonly Guid _workerId;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        INiftyDataService niftyDataService,
        ISaleAllocationStore saleContextDataStorage,
        IUtxoRetriever utxoRetriever,
        ISaleUtxoHandler saleUtxoHandler)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _niftyDataService = niftyDataService;
        _saleContextDataStorage = saleContextDataStorage;
        _utxoRetriever = utxoRetriever;
        _saleUtxoHandler = saleUtxoHandler;
        _workerId = Guid.NewGuid();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(EventIds.HostedServiceStarted, $"SaleWorker({_workerId}) started for Id: {_settings.CollectionId}");
        var collection = await _niftyDataService.GetCollectionAggregateAsync(_settings.CollectionId, ct);
        if (collection == null || collection.ActiveSales.Length == 0)
        {
            _logger.LogWarning(EventIds.DataServiceRetrievalWarning, $"Collection does not exist or there are no active sales!");
            _hostApplicationLifetime.StopApplication();
            return;
        }

        // TODO: Move from CollectionId to SaleId in MintsafeAppSettings and tie Worker to a Sale
        var saleContext = await _saleContextDataStorage.GetOrRestoreSaleContextAsync(collection, _workerId, ct);
        var totalNftsInRelease = saleContext.MintableTokens.Count + saleContext.AllocatedTokens.Count;
        if (totalNftsInRelease < saleContext.Sale.TotalReleaseQuantity)
        {
            _logger.LogWarning(EventIds.HostedServiceWarning, $"{collection.Collection.Name} has {totalNftsInRelease} total tokens which is less than {saleContext.Sale.TotalReleaseQuantity} sale release quantity.");
            _hostApplicationLifetime.StopApplication();
            return;
        }
        _logger.LogInformation(EventIds.HostedServiceInfo, $"SaleWorker({_workerId}) {collection.Collection.Name} has an active sale '{saleContext.Sale.Name}' for {saleContext.Sale.TotalReleaseQuantity} nifties (out of {totalNftsInRelease} total mintable and {saleContext.AllocatedTokens.Count} allocated) at {saleContext.Sale.SaleAddress}{Environment.NewLine}{saleContext.Sale.LovelacesPerToken} lovelaces per NFT ({saleContext.Sale.LovelacesPerToken / 1000000} ADA) and {saleContext.Sale.MaxAllowedPurchaseQuantity} max allowed");

        // TODO: Move away from single-threaded mutable saleContext 
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        do
        {
            var saleUtxos = await _utxoRetriever.GetUtxosAtAddressAsync(saleContext.Sale.SaleAddress, ct);
            _logger.LogDebug($"Querying SaleAddress UTxOs for sale {saleContext.Sale.Name} of {collection.Collection.Name} by {string.Join(",", collection.Collection.Publishers)}");
            _logger.LogDebug($"Found {saleUtxos.Length} UTxOs at {saleContext.Sale.SaleAddress}");
            foreach (var saleUtxo in saleUtxos)
            {
                if (saleContext.LockedUtxos.Contains(saleUtxo))
                {
                    _logger.LogDebug($"Utxo {saleUtxo.TxHash}[{saleUtxo.OutputIndex}]({saleUtxo.Lovelaces}) skipped (already locked)");
                    continue;
                }
                await _saleUtxoHandler.HandleAsync(saleUtxo, saleContext, ct);
            }
            _logger.LogDebug(
                $"Successful: {saleContext.SuccessfulUtxos.Count} UTxOs | Refunded: {saleContext.RefundedUtxos.Count} UTxOs | Failed: {saleContext.FailedUtxos.Count} UTxOs | Locked: {saleContext.LockedUtxos.Count} UTxOs");
            //_logger.LogDebug($"Allocated Tokens:\n\t\t{string.Join("\n\t\t", saleContext.AllocatedTokens.Select(t => t.AssetName))}");
        } while (await timer.WaitForNextTickAsync(ct));
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            EventIds.HostedServiceFinished, $"SaleWorker({_workerId}) BackgroundService is stopping");

        await base.StopAsync(stoppingToken);
    }
}