﻿using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess;
using Mintsafe.DataAccess.Composers;
using Mintsafe.DataAccess.Extensions;
using Mintsafe.DataAccess.Repositories;
using Mintsafe.DataAccess.Supporting;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<INiftyDataService, TableStorageDataService>();
        services.AddSingleton<INiftyCollectionRepository, NiftyCollectionRepository>();
        services.AddSingleton<INiftyRepository, NiftyRepository>();
        services.AddSingleton<ISaleRepository, SaleRepository>();
        services.AddSingleton<INiftyFileRepository, NiftyFileRepository>();

        services.AddSingleton<ICollectionAggregateComposer, CollectionAggregateComposer>();

        services.AddAzureClients(clientBuilder =>
        {
            var connectionString = "UseDevelopmentStorage=true";

            clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyCollection);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.Nifty);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.Sale);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyFile);
        });
    })
    .Build();

var pickupDirPath = $"C:\\ws\\temp\\cardadeer";

var collection = await LoadJsonFromFileAsync<NiftyCollection>(Path.Combine(pickupDirPath, "collection.json"));
var sale = await LoadJsonFromFileAsync<Sale>(Path.Combine(pickupDirPath, "sale.json"));
var nifties = await LoadDynamicJsonFromDirAsync(Path.Combine(pickupDirPath, "json")); // Assuming rares are also in here

BuildModelsAndInsertAsync(host.Services, collection, sale, nifties);

Console.WriteLine("Done");

await host.RunAsync();

async void BuildModelsAndInsertAsync(
    IServiceProvider services, 
    NiftyCollection niftyCollection, 
    Sale sale, 
    IList<JsonNode> niftyJson)
{
    using IServiceScope serviceScope = services.CreateScope();
    IServiceProvider provider = serviceScope.ServiceProvider;
    var dataService = provider.GetRequiredService<INiftyDataService>();

    var collectionId = niftyCollection.Id;
    var nifties = new List<Nifty>();
    foreach (var jsonObject in niftyJson)
    {
        var niftyId = Guid.NewGuid();
        var jsonArray = (JsonArray)jsonObject["attributes"];
        var jsonObjects = jsonArray.Cast<JsonObject>();
        var attributes =
            jsonObjects.Select(x => new KeyValuePair<string, string>((string) x["key"], (string) x["value"])).ToList();

        var nifty = new Nifty(
            Id: niftyId,
            CollectionId: collectionId,
            IsMintable: true,
            AssetName: $"Cardadeer{(int)jsonObject["edition"]}",
            Name: (string)jsonObject["name"],
            Description: (string)jsonObject["description"],
            Creators: new[] { "cardadeer.com" },
            Image: (string)jsonObject["image"],
            MediaType: "image/png",
            Files: Array.Empty<NiftyFile>(),
            CreatedAt: DateTimeOffset.FromUnixTimeMilliseconds((long)jsonObject["date"]).UtcDateTime,
            Royalty: new Royalty(0, ""), 
            Version: "1", 
            Attributes: attributes);

        nifties.Add(nifty);
    }

    var aggregate = new CollectionAggregate(niftyCollection, nifties.ToArray(), new[] { sale });

    await dataService.InsertCollectionAggregateAsync(aggregate, CancellationToken.None);
}

async Task<T> LoadJsonFromFileAsync<T>(string path)
{
    var raw = await File.ReadAllTextAsync(path);
    return JsonSerializer.Deserialize<T>(raw);
}

async Task<List<JsonNode>> LoadDynamicJsonFromDirAsync(string path)
{
    var files = Directory.GetFiles(path);
    var list = new List<JsonNode>();
    foreach (var filePath in files)
    {
        var raw = await File.ReadAllTextAsync(filePath);
        var model = JsonSerializer.Deserialize<JsonNode>(raw);
        list.Add(model);
    }

    return list.Where(x => x != null).ToList();
}