using Microsoft.Extensions.DependencyInjection;
using NiftyLaunchpad.ConsoleApp;
using NiftyLaunchpad.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;


var cts = ConsoleUtil.SetupUserInputCancellationTokenSource();
var settings = new NiftyLaunchpadSettings(
    Network: Network.Testnet,
    BlockFrostApiKey: "testneto96qDwlg4GaoKFfmKxPlHQhSkbea80cW",
    //BlockFrostApiKey: "mainnetGk6cqBgfG4nkQtvA1F80hJHfXzYQs8bW",
    //BasePath: @"C:\ws\temp\niftylaunchpad\",
    BasePath: "/home/knut/testnet-node/kc/niftylaunchpad/",
    PollingIntervalSeconds: 10);
var dataService = new NiftyDataService();

// Get { Collection * Sale[] * Token[] }
var collectionId = Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae");
var collection = await dataService.GetCollectionAggregateAsync(collectionId, cts.Token);
if (collection.ActiveSales.Length == 0)
{
    Console.WriteLine($"{collection.Collection.Name} with {collection.Tokens.Length} tokens has no active sales!");
    return;
}

var activeSale = collection.ActiveSales[0];
var mintableTokens = collection.Tokens.Where(t => t.IsMintable).ToList();
if (mintableTokens.Count == 0)
{
    Console.WriteLine($"{collection.Collection.Name} with {collection.Tokens.Length} tokens has no MINTABLE tokens!");
    return;
}
Console.WriteLine($"{collection.Collection.Name} has an active sale for {mintableTokens.Count} Nifties at {activeSale.SaleAddress}{Environment.NewLine}{activeSale.LovelacesPerToken} lovelaces per NFT and {activeSale.MaxAllowedPurchaseQuantity} max allowed");

var blockFrostClient = new BlockfrostClient(GetBlockFrostHttpClient(settings));
var utxoRetriever = new UtxoRetriever(settings);
var tokenAllocator = new TokenAllocator(settings, mintableTokens);
var tokenDistributor = new TokenDistributor(
    settings, 
    new MetadataGenerator(),
    new TxRetriever(blockFrostClient),
    new TxBuilder(settings), 
    new TxSubmitter(blockFrostClient));
var utxosLocked = new HashSet<string>();
var utxosSuccessfullyProcessed = new HashSet<string>();
var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollingIntervalSeconds));
var stopwatch = Stopwatch.StartNew();
try
{
do
{
    var saleUtxos = await utxoRetriever.GetUtxosAtAddressAsync(activeSale.SaleAddress, cts.Token);
    Console.WriteLine($"{stopwatch.ElapsedMilliseconds} Querying SaleAddress UTxOs for sale {activeSale.Name} of {collection.Collection.Name} by {string.Join(",",collection.Collection.Publishers)}");
    Console.WriteLine($"Found {saleUtxos.Length} UTxOs at {activeSale.SaleAddress}");

    foreach (var saleUtxo in saleUtxos)
    {
        if (utxosLocked.Contains(saleUtxo.ShortForm()))
        {
            Console.WriteLine($"Utxo {saleUtxo.TxHash}[{saleUtxo.OutputIndex}]({saleUtxo.Lovelaces()}) skipped (already locked)");
            continue;
        }

        try
        {
            var purchaseRequest = SalePurchaseGenerator.FromUtxo(saleUtxo, activeSale);
            Console.WriteLine($"Successfully built purchase request: {purchaseRequest.NiftyQuantityRequested} NFTs for {saleUtxo.Lovelaces()} and {purchaseRequest.ChangeInLovelace} change");

            var tokens = await tokenAllocator.AllocateTokensAsync(purchaseRequest, cts.Token);
            Console.WriteLine($"Successfully allocated {tokens.Length} tokens");

            var txHash = await tokenDistributor.DistributeNiftiesForSalePurchase(tokens, purchaseRequest, collection.Collection, activeSale, cts.Token);
            Console.WriteLine($"Successfully minted {tokens.Length} tokens from Tx {txHash}");

            utxosSuccessfullyProcessed.Add(saleUtxo.ShortForm());
        }
        catch (AllMintableTokensForSaleAllocated ex)
        {
            Console.Error.WriteLine(ex);
        }
        catch (SaleInactiveException ex)
        {
            Console.Error.WriteLine(ex);
        }
        catch (SalePeriodOutOfRangeException ex)
        {
            Console.Error.WriteLine(ex);
        }
        catch (InsufficientPaymentException ex)
        {
            Console.Error.WriteLine(ex);
        }
        catch (MaxAllowedPurchaseQuantityExceededException ex)
        {
            Console.Error.WriteLine(ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
        finally
        {
            utxosLocked.Add(saleUtxo.ShortForm());
        }
    }
    Console.WriteLine($"Successful: {utxosSuccessfullyProcessed.Count} UTxOs | Locked: {utxosLocked.Count} UTxOs");
} while (await timer.WaitForNextTickAsync(cts.Token));
}
catch (System.OperationCanceledException)
{
    Console.WriteLine(" User cancelled.. exiting");
}

static HttpClient GetBlockFrostHttpClient(NiftyLaunchpadSettings settings)
{
    // Easiest way to get an IHttpClientFactory is via Microsoft.Extensions.DependencyInjection.ServiceProvider
    var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("project_id", settings.BlockFrostApiKey);
    client.BaseAddress = settings.Network == Network.Mainnet
        ? new Uri("https://cardano-mainnet.blockfrost.io")
        : new Uri("https://cardano-testnet.blockfrost.io");
    return client;
}