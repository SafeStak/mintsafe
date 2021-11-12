using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using Mintsafe.SaleWorker;
using System;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();

        var settings = new MintsafeSaleWorkerSettings(
                Network: Network.Testnet,
                BlockFrostApiKey: "testneto96qDwlg4GaoKFfmKxPlHQhSkbea80cW",
                //BlockFrostApiKey: "mainnetGk6cqBgfG4nkQtvA1F80hJHfXzYQs8bW",
                BasePath: @"C:\ws\temp\niftylaunchpad\",
                //BasePath: "/home/knut/testnet-node/kc/mintsafe03/",
                PollingIntervalSeconds: 10,
                CollectionId: Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae"));
        services.AddSingleton(settings);

        services.AddHttpClient<BlockfrostClient>(nameof(BlockfrostClient), (s, client) =>
        {
            client.DefaultRequestHeaders.Add("project_id", settings.BlockFrostApiKey);
            client.BaseAddress = settings.Network == Network.Mainnet
                ? new Uri("https://cardano-mainnet.blockfrost.io")
                : new Uri("https://cardano-testnet.blockfrost.io");
        });

        services.AddSingleton<ISaleUtxoHandler, SaleUtxoHandler>();
        services.AddSingleton<INiftyAllocator, NiftyAllocator>();
        services.AddSingleton<IMetadataGenerator, MetadataGenerator>();
        services.AddSingleton<INiftyDistributor, NiftyDistributor>();
        services.AddSingleton<IUtxoRefunder, UtxoRefunder>();

        // Fakes
        services.AddSingleton<INiftyDataService, LocalNiftyDataService>();
        services.AddSingleton<IUtxoRetriever, FakeUtxoRetriever>();
        services.AddSingleton<ITxIoRetriever, FakeTxIoRetriever>();
        services.AddSingleton<ITxBuilder, FakeTxBuilder>();
        services.AddSingleton<ITxSubmitter, FakeTxSubmitter>();

        //// Reals
        //services.AddSingleton<IUtxoRetriever, CardanoCliUtxoRetriever>();
        //services.AddSingleton<ITxIoRetriever, BlockfrostTxIoRetriever>();
        //services.AddSingleton<ITxBuilder, CardanoCliTxBuilder>();
        //services.AddSingleton<ITxSubmitter, CardanoCliTxSubmitter>();
    })
    .Build();

await host.RunAsync();
