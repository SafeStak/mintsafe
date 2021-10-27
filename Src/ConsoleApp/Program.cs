using NiftyLaunchpad.ConsoleApp;
using NiftyLaunchpad.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

var cts = ConsoleUtil.SetupUserInputCancellationTokenSource();
var settings = new NiftyLaunchpadSettings(
    Network: Network.Testnet,
    PollingIntervalSeconds: 100);
var dataService = new NiftyDataService();
var utxoRetriever = new UtxoRetriever(settings);

var collectionId = Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae");
var sale = await dataService.GetSaleAggregateForCollectionAsync(collectionId, cts.Token);
var collection = await dataService.GetCollectionAsync(collectionId, cts.Token);

var salePurchaseRequests = new HashSet<NiftySalePurchaseRequest>();
var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollingIntervalSeconds));
var stopwatch = Stopwatch.StartNew();
do
{
    var saleUtxos = await utxoRetriever.GetUtxosAtAddressAsync(sale.SaleAddress);
    Console.WriteLine($"{stopwatch.ElapsedMilliseconds} {sale.Name} for {collection.Collection.Name}!");
    Console.WriteLine($"Found {saleUtxos.Length} UTxOs at {sale.SaleAddress}");

    foreach (var saleUtxo in saleUtxos)
    {
        try
        {
            var purchaseRequest = SalePurchaseRequester.FromUtxo(saleUtxo, sale);
            salePurchaseRequests.Add(purchaseRequest);
        }
        catch (InsufficientPaymentException ex)
        {
            Console.Error.WriteLine(ex);
        }
        catch (MaxAllowedPurchaseQuantityExceededException ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
    Console.WriteLine($"Currently Processing {salePurchaseRequests.Count}");
} while (await timer.WaitForNextTickAsync(cts.Token));
