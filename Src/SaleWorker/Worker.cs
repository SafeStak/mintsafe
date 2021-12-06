using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.SaleWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly INiftyDataService _niftyDataService;
    private readonly IUtxoRetriever _utxoRetriever;
    private readonly ISaleUtxoHandler _saleUtxoHandler;
    private readonly Guid _workerId;

    public Worker(
        ILogger<Worker> logger,
        MintsafeAppSettings settings,
        INiftyDataService niftyDataService,
        IUtxoRetriever utxoRetriever,
        ISaleUtxoHandler saleUtxoHandler)
    {
        _logger = logger;
        _settings = settings;
        _niftyDataService = niftyDataService;
        _utxoRetriever = utxoRetriever;
        _saleUtxoHandler = saleUtxoHandler;
        _workerId = Guid.NewGuid();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(EventIds.HostedServiceStarted, $"SaleWorker({_workerId}) started for Id: {_settings.CollectionId}");
        var collection = await _niftyDataService.GetCollectionAggregateAsync(_settings.CollectionId, ct);
        if (collection.ActiveSales.Length == 0)
        {
            _logger.LogWarning(EventIds.HostedServiceWarning, $"{collection.Collection.Name} with {collection.Tokens.Length} mintable tokens has no active sales!");
            await base.StopAsync(ct);
            return;
        }

        // TODO: Move from CollectionId to SaleId in MintsafeAppSettings and tie Worker to a Sale
        var activeSale = collection.ActiveSales[0];
        var mintableTokens = collection.Tokens.Where(t => t.IsMintable).ToList();
        if (mintableTokens.Count < activeSale.TotalReleaseQuantity)
        {
            _logger.LogWarning(EventIds.HostedServiceWarning, $"{collection.Collection.Name} has {mintableTokens.Count} mintable tokens which is less than {activeSale.TotalReleaseQuantity} sale release quantity.");
            await base.StopAsync(ct);
            return;
        }
        _logger.LogInformation(EventIds.HostedServiceInfo, $"SaleWorker({_workerId}) {collection.Collection.Name} has an active sale '{activeSale.Name}' for {activeSale.TotalReleaseQuantity} nifties (out of {mintableTokens.Count} total mintable) at {activeSale.SaleAddress}{Environment.NewLine}{activeSale.LovelacesPerToken} lovelaces per NFT ({activeSale.LovelacesPerToken / 1000000} ADA) and {activeSale.MaxAllowedPurchaseQuantity} max allowed");

        // TODO: Move away from single-threaded mutable saleContext that isn't crash tolerant
        // In other words, we need to persist the state after every allocation and read it when the worker runs
        var saleContext = GetSaleContext(mintableTokens, activeSale, collection.Collection);
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        do
        {
            var saleUtxos = await _utxoRetriever.GetUtxosAtAddressAsync(activeSale.SaleAddress, ct);
            _logger.LogDebug($"Querying SaleAddress UTxOs for sale {activeSale.Name} of {collection.Collection.Name} by {string.Join(",", collection.Collection.Publishers)}");
            _logger.LogDebug($"Found {saleUtxos.Length} UTxOs at {activeSale.SaleAddress}");
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
                $"Successful: {saleContext.SuccessfulUtxos.Count} UTxOs | Refunded: {saleContext.RefundedUtxos.Count} | Locked: {saleContext.LockedUtxos.Count} UTxOs");
            _logger.LogDebug($"Allocated Tokens:\n{string.Join('\n', saleContext.AllocatedTokens.Select(t => t.AssetName))}");
        } while (await timer.WaitForNextTickAsync(ct));
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            EventIds.HostedServiceFinished, $"SaleWorker({_workerId}) BackgroundService is stopping");

        await base.StopAsync(stoppingToken);
    }

    private SaleContext GetSaleContext(
        List<Nifty> mintableTokens, Sale sale,  NiftyCollection collection)
    {
        var saleContext = new SaleContext(
            _workerId,
            sale,
            collection,
            mintableTokens, 
            new List<Nifty>(), 
            new HashSet<Utxo>(), 
            new HashSet<Utxo>(), 
            new HashSet<Utxo>());

        return saleContext;
    }
}