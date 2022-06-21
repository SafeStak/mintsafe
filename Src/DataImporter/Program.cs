using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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

        services.AddSingleton<IAggregateComposer, AggregateComposer>();

        services.AddAzureClients(clientBuilder =>
        {
            var connectionString = "";

            clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyCollection);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.Nifty);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.Sale);
            clientBuilder.AddTableClient(connectionString, Constants.TableNames.NiftyFile);
        });
    })
    .Build();

var pickupDirPath = $"C:\\ws\\temp\\tacf_portrait2";

var collection = await LoadJsonFromFileAsync<NiftyCollection>(Path.Combine(pickupDirPath, "collection_tn.json"));
var sale = await LoadJsonFromFileAsync<Sale>(Path.Combine(pickupDirPath, "sale_tn.json"));
var nifties = await LoadDynamicJsonFromFileAsync(Path.Combine(pickupDirPath, "nifties.json")); 

BuildModelsAndInsertAsync(host.Services, collection, sale, nifties);

Console.WriteLine("Done");

await host.RunAsync();

async void BuildModelsAndInsertAsync(
    IServiceProvider services, 
    NiftyCollection niftyCollection, 
    Sale sale, 
    IList<JsonNode?> niftyJson)
{
    using IServiceScope serviceScope = services.CreateScope();
    IServiceProvider provider = serviceScope.ServiceProvider;
    var dataService = provider.GetRequiredService<INiftyDataService>();

    var collectionId = niftyCollection.Id;
    var nifties = new List<Nifty>();
    foreach (var jsonObject in niftyJson)
    {
        var niftyId = Guid.NewGuid();
        var attributesJsonArray = (JsonArray)jsonObject["Attributes"];
        var attributesJsonObjectsArray = attributesJsonArray.Cast<JsonObject>();
        var attributes =
            attributesJsonObjectsArray
                .Select(x => new KeyValuePair<string, string>((string) x["Key"], (string) x["Value"])).ToArray();
        var creators = ((JsonArray)jsonObject["Creators"]).Cast<JsonValue>()
            .Select(jv => (string)jv ?? string.Empty)
            .ToArray();

        DateTime.TryParseExact(
            (string)jsonObject["CreatedAt"],
            @"yyyy-MM-dd\THH:mm:ss\Z",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var dateTimeParsed);

        var nifty = new Nifty(
            Id: niftyId,
            CollectionId: collectionId,
            IsMintable: true,
            AssetName: (string)jsonObject["AssetName"],
            Name: (string)jsonObject["Name"],
            Description: (string)jsonObject["Description"],
            Creators: creators,
            Image: (string)jsonObject["Image"],
            MediaType: (string)jsonObject["MediaType"],
            Files: Array.Empty<NiftyFile>(),
            CreatedAt: dateTimeParsed,
            Version: (string)jsonObject["Version"], 
            Attributes: attributes);

        nifties.Add(nifty);
    }

    var aggregate = new ProjectAggregate(niftyCollection, nifties.ToArray(), new[] { sale });

    //await Task.Delay(100);
    await dataService.InsertCollectionAggregateAsync(aggregate, CancellationToken.None);
}

async Task<T?> LoadJsonFromFileAsync<T>(string path)
{
    var raw = await File.ReadAllTextAsync(path).ConfigureAwait(false);
    return JsonSerializer.Deserialize<T>(raw, SerialiserOptions());
}


async Task<List<JsonNode?>> LoadDynamicJsonFromFileAsync(string filePath)
{
    var raw = await File.ReadAllTextAsync(filePath);
    var list = JsonSerializer.Deserialize<List<JsonNode?>>(raw, SerialiserOptions());
    return list.Where(x => x != null).ToList();
}

async Task<List<JsonNode?>> LoadDynamicJsonFromDirAsync(string path)
{
    var files = Directory.GetFiles(path);
    var list = new List<JsonNode?>();
    foreach (var filePath in files)
    {
        var raw = await File.ReadAllTextAsync(filePath);
        var model = JsonSerializer.Deserialize<JsonNode>(raw, SerialiserOptions());
        list.Add(model);
    }

    return list.Where(x => x != null).ToList();
}


static JsonSerializerOptions SerialiserOptions() => new()
{
    //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};