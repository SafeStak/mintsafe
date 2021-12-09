using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using Mintsafe.SaleWorker;
using System;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(configHost =>
    {
        configHost
            .AddJsonFile("hostsettings.json", optional: true);
    })
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json",
                optional: true, reloadOnChange: true);
    })
    .ConfigureLogging((hostContext, logging) =>
    {
        logging
            .ClearProviders()
            .AddConfiguration(hostContext.Configuration.GetSection("Logging"))
            .AddConsole();
        var appInsightsConfig = hostContext.Configuration
            .GetSection("ApplicationInsights")
            .Get<ApplicationInsightsConfig>();
        if (appInsightsConfig.Enabled && !string.IsNullOrWhiteSpace(appInsightsConfig.InstrumentationKey))
        {
            logging.AddApplicationInsights(appInsightsConfig.InstrumentationKey);
        }
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();

        // Read config
        var cardanoNetworkConfig = hostContext.Configuration
            .GetSection("CardanoNetwork")
            .Get<CardanoNetworkConfig>();

        var blockfrostApiConfig = hostContext.Configuration
            .GetSection("BlockfrostApi")
            .Get<BlockfrostApiConfig>();
        if (blockfrostApiConfig.BaseUrl == null)
            throw new MintSafeConfigException("BaseUrl is missing in BlockfrostApiConfig", "MintsafeWorker.BlockfrostApiConfig");

        var mintsafeWorkerConfig = hostContext.Configuration
            .GetSection("MintsafeWorker")
            .Get<MintsafeWorkerConfig>();
        if (mintsafeWorkerConfig.CollectionId == null)
            throw new MintSafeConfigException("CollectionId is missing in MintsafeWorkerConfig", "MintsafeWorker.CollectionId");
        var settings = new MintsafeAppSettings
        {
            Network = cardanoNetworkConfig.Network == "Mainnet" ? Network.Mainnet : Network.Testnet,
            BlockFrostApiKey = blockfrostApiConfig.ApiKey,
            BasePath = mintsafeWorkerConfig.MintBasePath,
            //BasePath = "/home/knut/testnet-node/kc/mintsafe03/",
            PollingIntervalSeconds = mintsafeWorkerConfig.PollingIntervalSeconds.HasValue ? mintsafeWorkerConfig.PollingIntervalSeconds.Value : 10,
            CollectionId = Guid.Parse(mintsafeWorkerConfig.CollectionId)
        };
        services.AddSingleton(settings);

        var appInsightsConfig = hostContext.Configuration
            .GetSection("ApplicationInsights")
            .Get<ApplicationInsightsConfig>();
        if (appInsightsConfig.Enabled && !string.IsNullOrWhiteSpace(appInsightsConfig.InstrumentationKey))
        {
            var aiOptions = new ApplicationInsightsServiceOptions
            {
                InstrumentationKey = appInsightsConfig.InstrumentationKey,
                EnableDependencyTrackingTelemetryModule = false,
            };
            services.AddApplicationInsightsTelemetryWorkerService(aiOptions);
            services.AddSingleton<IInstrumentor, AppInsightsInstrumentor>();
        }
        else
        {
            services.AddSingleton<IInstrumentor, LoggingInstrumentor>();
        }

        services.AddHttpClient<BlockfrostClient>(
            nameof(BlockfrostClient), 
            (s, client) =>
            {
                client.DefaultRequestHeaders.Add("project_id", blockfrostApiConfig.ApiKey);
                client.BaseAddress = new Uri(blockfrostApiConfig.BaseUrl);
            });

        services.AddSingleton<ISaleUtxoHandler, SaleUtxoHandler>();
        services.AddSingleton<INiftyAllocator, NiftyAllocator>();
        services.AddSingleton<IMetadataFileGenerator, MetadataFileGenerator>();
        services.AddSingleton<IMetadataJsonBuilder, MetadataJsonBuilder>();
        services.AddSingleton<INiftyDistributor, NiftyDistributor>();
        services.AddSingleton<IUtxoRefunder, UtxoRefunder>();

        // Fakes
        services.AddSingleton<INiftyDataService, LocalNiftyDataService>();
        //services.AddSingleton<IUtxoRetriever, FakeUtxoRetriever>();
        //services.AddSingleton<ITxInfoRetriever, FakeTxIoRetriever>();
        services.AddSingleton<ITxBuilder, FakeTxBuilder>();
        services.AddSingleton<ITxSubmitter, FakeTxSubmitter>();

        //// Reals
        //services.AddSingleton<IUtxoRetriever, CardanoCliUtxoRetriever>();
        services.AddSingleton<IUtxoRetriever, BlockfrostUtxoRetriever>();
        services.AddSingleton<ITxInfoRetriever, BlockfrostTxInfoRetriever>();
        //services.AddSingleton<ITxBuilder, CardanoCliTxBuilder>();
        //services.AddSingleton<ITxSubmitter, CardanoCliTxSubmitter>();
    })
    .Build();

await host.RunAsync();
