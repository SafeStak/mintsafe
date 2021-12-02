using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using Microsoft.Extensions.Azure;
using Mintsafe.DataAccess;
using Mintsafe.DataAccess.Composers;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Repositories;
using Mintsafe.DataAccess.Supporting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
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
builder.Services.AddSingleton<IMetadataJsonBuilder, MetadataJsonBuilder>();
builder.Services.AddSingleton<INiftyDistributor, NiftyDistributor>();
builder.Services.AddSingleton<IUtxoRefunder, UtxoRefunder>();
builder.Services.AddSingleton<IYoloWalletService, YoloWalletService>();

// Data Access
builder.Services.AddSingleton<INiftyDataService, TableStorageDataService>();
builder.Services.AddSingleton<INiftyCollectionRepository, NiftyCollectionRepository>();
builder.Services.AddSingleton<INiftyRepository, NiftyRepository>();
builder.Services.AddSingleton<ISaleRepository, SaleRepository>();
builder.Services.AddSingleton<INiftyFileRepository, NiftyFileRepository>();

builder.Services.AddSingleton<ICollectionAggregateComposer, CollectionAggregateComposer>();

builder.Services.AddAzureClients(clientBuilder =>
{
    var connectionString = builder.Configuration.GetSection("Storage:ConnectionString").Value;
    
    clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyCollection);
    clientBuilder.AddTableClient(connectionString, Constants.TableNames.Nifty);
    clientBuilder.AddTableClient(connectionString, Constants.TableNames.Sale);
    clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyFile);
});

//TODO
builder.Services.AddSingleton<IMetadataJsonBuilder, MetadataJsonBuilder>();

// Fakes
//builder.Services.AddSingleton<IUtxoRetriever, FakeUtxoRetriever>();
builder.Services.AddSingleton<ITxInfoRetriever, FakeTxIoRetriever>();
builder.Services.AddSingleton<ITxBuilder, FakeTxBuilder>();
builder.Services.AddSingleton<ITxSubmitter, FakeTxSubmitter>();

// Reals
builder.Services.AddSingleton<IUtxoRetriever, BlockfrostUtxoRetriever>();
//services.AddSingleton<IUtxoRetriever, CardanoCliUtxoRetriever>();
//services.AddSingleton<ITxIoRetriever, BlockfrostTxIoRetriever>();
//services.AddSingleton<ITxBuilder, CardanoCliTxBuilder>();
//services.AddSingleton<ITxSubmitter, CardanoCliTxSubmitter>();

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

app.UseCors();

app.Run();
