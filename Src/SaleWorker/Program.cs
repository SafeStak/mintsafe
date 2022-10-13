using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess;
using Mintsafe.DataAccess.Composers;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Repositories;
using Mintsafe.DataAccess.Supporting;
using Mintsafe.Lib;
using Mintsafe.SaleWorker;
using System;
using System.Linq;

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
            throw new MintSafeConfigException("BaseUrl is missing in BlockfrostApiConfig", "MintsafeWorker.BlockfrostApi");

        var keychainConfig = hostContext.Configuration
            .GetSection("Keychain")
            .Get<KeychainConfig>();
        if (string.IsNullOrWhiteSpace(keychainConfig.KeyVaultUrl))
            throw new MintSafeConfigException("KeyVaultUrl is missing in KeychainConfig", "MintsafeWorker.Keychain.KeyVaultUrl");

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
            PollingIntervalSeconds = mintsafeWorkerConfig.PollingIntervalSeconds.HasValue ? mintsafeWorkerConfig.PollingIntervalSeconds.Value : 20,
            PollErrorRetryLimit = mintsafeWorkerConfig.PollErrorRetryLimit.HasValue ? mintsafeWorkerConfig.PollErrorRetryLimit.Value : 8,
            CollectionId = Guid.Parse(mintsafeWorkerConfig.CollectionId),
            SaleIds = mintsafeWorkerConfig.SaleIds?.Select(Guid.Parse).ToArray() ?? Array.Empty<Guid>(),
            KeyVaultUrl = keychainConfig.KeyVaultUrl
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
        services.AddSingleton<INiftyDistributor, CardanoSharpNiftyDistributor>();
        services.AddSingleton<IUtxoRefunder, UtxoRefunder>();
        services.AddSingleton<ISaleAllocationStore, SaleAllocationFileStore>();

        // Fakes
        //services.AddSingleton<INiftyDataService, LocalNiftyDataService>();
        //services.AddSingleton<IUtxoRetriever, FakeUtxoRetriever>();
        //services.AddSingleton<ITxInfoRetriever, FakeTxIoRetriever>();
        //services.AddSingleton<ITxBuilder, FakeTxBuilder>();
        //services.AddSingleton<ITxSubmitter, FakeTxSubmitter>();

        // Reals
        services.AddSingleton<INetworkContextRetriever, BlockfrostNetworkContextRetriever>();
        services.AddSingleton<IBlockfrostClient, BlockfrostClient>();
        services.AddSingleton<IMintingKeychainRetriever, KeyVaultMintingKeychainRetriever>();
        services.AddSingleton<IUtxoRetriever, BlockfrostUtxoRetriever>();
        services.AddSingleton<ITxInfoRetriever, BlockfrostTxInfoRetriever>();
        services.AddSingleton<IMintTransactionBuilder, CardanoSharpTxBuilder>();
        services.AddSingleton<ITxSubmitter, BlockfrostTxSubmitter>();
        services.AddSingleton<INiftyDataService, TableStorageDataService>();
        services.AddAzureClients(clientBuilder =>
        {
            var connectionString = hostContext.Configuration.GetSection("Storage:ConnectionString").Value;
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyCollection);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.Nifty);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.Sale);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyFile);
        });
        services.AddSingleton<INiftyDataService, TableStorageDataService>();
        services.AddSingleton<IAggregateComposer, AggregateComposer>();
        services.AddSingleton<INiftyCollectionRepository, NiftyCollectionRepository>();
        services.AddSingleton<INiftyRepository, NiftyRepository>();
        services.AddSingleton<ISaleRepository, SaleRepository>();
        services.AddSingleton<INiftyFileRepository, NiftyFileRepository>();
    })
    .Build();

await host.RunAsync();
