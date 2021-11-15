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
    private readonly MintsafeSaleWorkerSettings _settings;
    private readonly INiftyDataService _niftyDataService;
    private readonly IUtxoRetriever _utxoRetriever;
    private readonly ISaleUtxoHandler _saleUtxoHandler;

    public Worker(
        ILogger<Worker> logger,
        MintsafeSaleWorkerSettings settings,
        INiftyDataService niftyDataService,
        IUtxoRetriever utxoRetriever,
        ISaleUtxoHandler saleUtxoHandler)
    {
        _logger = logger;
        _settings = settings;
        _niftyDataService = niftyDataService;
        _utxoRetriever = utxoRetriever;
        _saleUtxoHandler = saleUtxoHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var collection = await _niftyDataService.GetCollectionAggregateAsync(_settings.CollectionId, ct);
        if (collection.ActiveSales.Length == 0)
        {
            _logger.LogWarning($"{collection.Collection.Name} with {collection.Tokens.Length} mintable tokens has no active sales!");
            return;
        }

        var activeSale = collection.ActiveSales[0];
        var mintableTokens = collection.Tokens.Where(t => t.IsMintable).ToList();
        if (mintableTokens.Count < activeSale.TotalReleaseQuantity)
        {
            _logger.LogWarning($"{collection.Collection.Name} has {mintableTokens.Count} mintable tokens which is less than {activeSale.TotalReleaseQuantity} sale release quantity.");
            return;
        }
        _logger.LogInformation($"{collection.Collection.Name} has an active sale '{activeSale.Name}' for {activeSale.TotalReleaseQuantity} nifties (out of {mintableTokens.Count} total mintable) at {activeSale.SaleAddress}{Environment.NewLine}{activeSale.LovelacesPerToken} lovelaces per NFT ({activeSale.LovelacesPerToken / 1000000} ADA) and {activeSale.MaxAllowedPurchaseQuantity} max allowed");

        // TODO: Move away from single-threaded mutable saleContext that isn't crash tolerant
        // In other words, we need to persist the state after every allocation and read it when the worker runs
        var saleContext = new SaleContext(mintableTokens, new List<Nifty>(), new HashSet<Utxo>(), new HashSet<Utxo>(), new HashSet<Utxo>());
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        do
        {
            var saleUtxos = await _utxoRetriever.GetUtxosAtAddressAsync(activeSale.SaleAddress, ct);
            _logger.LogInformation($"Querying SaleAddress UTxOs for sale {activeSale.Name} of {collection.Collection.Name} by {string.Join(",", collection.Collection.Publishers)}");
            _logger.LogInformation($"Found {saleUtxos.Length} UTxOs at {activeSale.SaleAddress}");
            foreach (var saleUtxo in saleUtxos)
            {
                if (saleContext.LockedUtxos.Contains(saleUtxo))
                {
                    _logger.LogInformation($"Utxo {saleUtxo.TxHash}[{saleUtxo.OutputIndex}]({saleUtxo.Lovelaces}) skipped (already locked)");
                    continue;
                }
                await _saleUtxoHandler.HandleAsync(saleUtxo, collection.Collection, activeSale, saleContext, ct);
            }
            _logger.LogInformation(
                $"Successful: {saleContext.SuccessfulUtxos.Count} UTxOs | Refunded: {saleContext.RefundedUtxos.Count} | Locked: {saleContext.LockedUtxos.Count} UTxOs");
            //_logger.LogInformation($"Allocated Tokens:\n{string.Join('\n', saleContext.AllocatedTokens.Select(t => t.AssetName))}");
        } while (await timer.WaitForNextTickAsync(ct));
    }
}