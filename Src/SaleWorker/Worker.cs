using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.SaleWorker;

public class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<Worker> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly INiftyDataService _niftyDataService;
    private readonly ISaleAllocationStore _saleContextDataStorage;
    private readonly INetworkContextRetriever _networkContextRetriever;
    private readonly IUtxoRetriever _utxoRetriever;
    private readonly ISaleUtxoHandler _saleUtxoHandler;
    private readonly Guid _workerId;

    public Worker(
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<Worker> logger,
        MintsafeAppSettings settings,
        INiftyDataService niftyDataService,
        ISaleAllocationStore saleContextDataStorage,
        INetworkContextRetriever networkContextRetriever,
        IUtxoRetriever utxoRetriever,
        ISaleUtxoHandler saleUtxoHandler)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _settings = settings;
        _niftyDataService = niftyDataService;
        _saleContextDataStorage = saleContextDataStorage;
        _networkContextRetriever = networkContextRetriever;
        _utxoRetriever = utxoRetriever;
        _saleUtxoHandler = saleUtxoHandler;
        _workerId = Guid.NewGuid();
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(EventIds.HostedServiceStarted, $"SaleWorker({_workerId}) started for CollectionId: {_settings.CollectionId}, Ids: {string.Join(',', _settings.SaleIds)}");
        var saleAggregates = await Task.WhenAll(_settings.SaleIds.Select(saleId => _niftyDataService.GetSaleAggregateAsync(saleId, ct)));
        var saleContexts = await Task.WhenAll(saleAggregates.Select(s => _saleContextDataStorage.GetOrRestoreSaleContextAsync(s, _workerId, ct)));
        var activeSales = saleContexts.Where(s => (s.Sale.TotalReleaseQuantity - s.AllocatedTokens.Count) > 0).ToArray();
        if (!activeSales.Any())
        {
            _logger.LogWarning(EventIds.HostedServiceWarning, $"No active sales with tokens remaining found");
            _hostApplicationLifetime.StopApplication();
            return;
        }
        _logger.LogInformation(EventIds.HostedServiceInfo, $"SaleWorker({_workerId}) has an {activeSales.Length} activeSales{Environment.NewLine}{string.Join(Environment.NewLine, activeSales.Select(GetSaleInfo))}");

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        do
        {
            var networkContext = await _networkContextRetriever.GetNetworkContext(ct);
            await Task.WhenAll(activeSales.Select(s => PollSaleAddressForUtxos(s, networkContext, ct)));
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task PollSaleAddressForUtxos(SaleContext saleContext, NetworkContext networkContext, CancellationToken ct)
    {
        var saleUtxos = await _utxoRetriever.GetUtxosAtAddressAsync(saleContext.Sale.SaleAddress, ct);
        _logger.LogDebug($"Querying SaleAddress UTxOs for sale {saleContext.Sale.Name} of {saleContext.Collection.Name} by {string.Join(",", saleContext.Collection.Publishers)}");
        _logger.LogDebug($"Found {saleUtxos.Length} UTxOs at {saleContext.Sale.SaleAddress}");
        foreach (var saleUtxo in saleUtxos)
        {
            if (saleContext.LockedUtxos.Contains(saleUtxo))
            {
                _logger.LogDebug($"Utxo {saleUtxo.TxHash}[{saleUtxo.OutputIndex}]({saleUtxo.Lovelaces}) skipped (already locked)");
                continue;
            }
            await _saleUtxoHandler.HandleAsync(saleUtxo, saleContext, networkContext, ct);
        }
        _logger.LogDebug(
            $"Successful: {saleContext.SuccessfulUtxos.Count} UTxOs | Refunded: {saleContext.RefundedUtxos.Count} UTxOs | Failed: {saleContext.FailedUtxos.Count} UTxOs | Locked: {saleContext.LockedUtxos.Count} UTxOs");
    }

    private static string GetSaleInfo(SaleContext saleContext)
    {
        return $"{saleContext.Sale.TotalReleaseQuantity} NFTs ({saleContext.AllocatedTokens.Count} allocated) at {saleContext.Sale.SaleAddress} Cost {saleContext.Sale.LovelacesPerToken / 1000000} ADA and {saleContext.Sale.MaxAllowedPurchaseQuantity} max allowed";
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            EventIds.HostedServiceFinished, $"SaleWorker({_workerId}) BackgroundService is stopping");

        await base.StopAsync(stoppingToken);
    }
}