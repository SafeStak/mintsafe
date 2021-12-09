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
    private readonly IUtxoRetriever _utxoRetriever;
    private readonly ISaleUtxoHandler _saleUtxoHandler;
    private readonly Guid _workerId;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        INiftyDataService niftyDataService,
        IUtxoRetriever utxoRetriever,
        ISaleUtxoHandler saleUtxoHandler)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _instrumentor = instrumentor;
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
            _hostApplicationLifetime.StopApplication();
            return;
        }

        // TODO: Move from CollectionId to SaleId in MintsafeAppSettings and tie Worker to a Sale
        var activeSale = collection.ActiveSales[0];
        var mintableTokens = collection.Tokens.Where(t => t.IsMintable).ToList();
        if (mintableTokens.Count < activeSale.TotalReleaseQuantity)
        {
            _logger.LogWarning(EventIds.HostedServiceWarning, $"{collection.Collection.Name} has {mintableTokens.Count} mintable tokens which is less than {activeSale.TotalReleaseQuantity} sale release quantity.");
            _hostApplicationLifetime.StopApplication();
            return;
        }
        _logger.LogInformation(EventIds.HostedServiceInfo, $"SaleWorker({_workerId}) {collection.Collection.Name} has an active sale '{activeSale.Name}' for {activeSale.TotalReleaseQuantity} nifties (out of {mintableTokens.Count} total mintable) at {activeSale.SaleAddress}{Environment.NewLine}{activeSale.LovelacesPerToken} lovelaces per NFT ({activeSale.LovelacesPerToken / 1000000} ADA) and {activeSale.MaxAllowedPurchaseQuantity} max allowed");

        // TODO: Move away from single-threaded mutable saleContext that isn't crash tolerant
        // In other words, we need to persist the state after every allocation and read it when the worker runs
        var saleContext = GetOrRestoreSaleContext(mintableTokens, activeSale, collection.Collection);
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

    private SaleContext GetOrRestoreSaleContext(
        List<Nifty> mintableTokens, Sale sale,  NiftyCollection collection)
    {
        var saleFolder = Path.Combine(_settings.BasePath, sale.Id.ToString()[..8]);
        var saleUtxosFolder = Path.Combine(saleFolder, "utxos");
        var mintableNftIdsSnapshotPath = Path.Combine(saleFolder, "mintableNftIds.csv");
        var allocatedNftIdsPath = Path.Combine(saleFolder, "allocatedNftIds.csv");
        var sw = new Stopwatch();
        // Brand new sale for worker
        if (!Directory.Exists(saleFolder))
        {
            Directory.CreateDirectory(saleFolder);
            Directory.CreateDirectory(saleUtxosFolder);
            sw.Start();
            File.WriteAllLines(mintableNftIdsSnapshotPath, mintableTokens.Select(n => n.Id.ToString()));
            File.WriteAllText(allocatedNftIdsPath, string.Empty);
            _instrumentor.TrackDependency(
                EventIds.SaleContextWriteElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(Worker),
                mintableNftIdsSnapshotPath,
                "WriteSaleContextMintable",
                isSuccessful: true);
            return new SaleContext(
                _workerId,
                saleFolder,
                saleUtxosFolder,
                sale,
                collection,
                mintableTokens,
                new List<Nifty>(),
                new HashSet<Utxo>(),
                new HashSet<Utxo>(),
                new HashSet<Utxo>());
        }
        // Restore sale context from previous execution - starting with snapshot of all mintable NFTs at start of sale
        sw.Restart();
        var allocatedNftIds = new HashSet<string>(File.ReadAllLines(allocatedNftIdsPath));
        _instrumentor.TrackDependency(
            EventIds.SaleContextRestoreElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(Worker),
            allocatedNftIdsPath,
            "ReadSaleContextAllocated",
            isSuccessful: true);
        var revisedMintableNfts = new List<Nifty>();
        var allocatedNfts = new List<Nifty>();
        foreach (var nft in mintableTokens)
        {
            if (allocatedNftIds.Contains(nft.Id.ToString()))
                allocatedNfts.Add(nft);
            else
                revisedMintableNfts.Add(nft);
        }

        var saleContext = new SaleContext(
            _workerId,
            saleFolder,
            saleUtxosFolder,
            sale,
            collection,
            revisedMintableNfts,
            allocatedNfts, 
            new HashSet<Utxo>(), 
            new HashSet<Utxo>(), 
            new HashSet<Utxo>());

        return saleContext;
    }
}