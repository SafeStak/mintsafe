using Microsoft.Extensions.DependencyInjection;
using Mintsafe.Abstractions;
using Mintsafe.ConsoleApp;
using Mintsafe.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;

var cts = ConsoleUtil.SetupUserInputCancellationTokenSource();

try
{
       
}
catch (OperationCanceledException)
{
    Console.WriteLine(" User cancelled.. exiting");
}

static HttpClient GetBlockFrostHttpClient(MintsafeSaleWorkerSettings settings)
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