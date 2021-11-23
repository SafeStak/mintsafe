using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess;
using Mintsafe.DataAccess.Mapping;
using Mintsafe.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var settings = new MintsafeAppSettings(
                Network: Network.Testnet,
                BlockFrostApiKey: "testneto96qDwlg4GaoKFfmKxPlHQhSkbea80cW",
                //BlockFrostApiKey: "mainnetGk6cqBgfG4nkQtvA1F80hJHfXzYQs8bW",
                BasePath: @"C:\ws\temp\niftylaunchpad\",
                //BasePath: "/home/knut/testnet-node/kc/mintsafe03/",
                PollingIntervalSeconds: 10,
                CollectionId: Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae"));
builder.Services.AddSingleton(settings);

builder.Services.AddHttpClient<BlockfrostClient>(nameof(BlockfrostClient), (s, client) =>
{
    client.DefaultRequestHeaders.Add("project_id", settings.BlockFrostApiKey);
    client.BaseAddress = settings.Network == Network.Mainnet
        ? new Uri("https://cardano-mainnet.blockfrost.io")
        : new Uri("https://cardano-testnet.blockfrost.io");
});

builder.Services.AddSingleton<INiftyAllocator, NiftyAllocator>();
builder.Services.AddSingleton<IMetadataFileGenerator, MetadataFileGenerator>();
builder.Services.AddSingleton<INiftyDistributor, NiftyDistributor>();
builder.Services.AddSingleton<IUtxoRefunder, UtxoRefunder>();

// Data Access

builder.Services.AddSingleton<INiftyDataService, TableStorageDataService>();
builder.Services.AddSingleton<INiftyCollectionRepository, NiftyCollectionRepository>();
builder.Services.AddSingleton<INiftyCollectionMapper, NiftyCollectionMapper>();
builder.Services.AddSingleton<INiftyRepository, NiftyRepository>();
builder.Services.AddSingleton<INiftyMapper, NiftyMapper>();
builder.Services.AddSingleton<ISaleRepository, SaleRepository>();
builder.Services.AddSingleton<ISaleMapper, SaleMapper>();

builder.Services.AddAzureClients(clientBuilder =>
{
    var connectionString = builder.Configuration.GetSection("Storage:ConnectionString").Value;

    //TODO Cleanup here try and use .AddTableClient
    
    clientBuilder.AddClient<TableClient, TableClientOptions>((provider, credential, options) =>
    {
        var tableClient = new TableClient(connectionString, "NiftyCollection");
        tableClient.CreateIfNotExists();
        return tableClient;
    }).WithName("NiftyCollection");

    clientBuilder.AddClient<TableClient, TableClientOptions>((provider, credential, options) =>
    {
        var tableClient = new TableClient(connectionString, "Nifty");
        tableClient.CreateIfNotExists();
        return tableClient;
    }).WithName("Nifty");

    clientBuilder.AddClient<TableClient, TableClientOptions>((provider, credential, options) =>
    {
        var tableClient = new TableClient(connectionString, "Sale");
        tableClient.CreateIfNotExists();
        return tableClient;
    }).WithName("Sale");

    // Use DefaultAzureCredential by default
    clientBuilder.UseCredential(new DefaultAzureCredential());

    // Set up any default settings
    clientBuilder.ConfigureDefaults(builder.Configuration.GetSection("AzureDefaults"));
});

//TODO
builder.Services.AddSingleton<IMetadataJsonBuilder, MetadataJsonBuilder>();

// Fakes
//builder.Services.AddSingleton<INiftyDataService, LocalNiftyDataService>();
builder.Services.AddSingleton<IUtxoRetriever, FakeUtxoRetriever>();
builder.Services.AddSingleton<ITxInfoRetriever, FakeTxIoRetriever>();
builder.Services.AddSingleton<ITxBuilder, FakeTxBuilder>();
builder.Services.AddSingleton<ITxSubmitter, FakeTxSubmitter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.Run();
