using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using Mintsafe.WasmApp;
using Mintsafe.WasmApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var settings = new MintsafeAppSettings
{
    Network = Network.Testnet,
    BlockFrostApiKey = "testneto96qDwlg4GaoKFfmKxPlHQhSkbea80cW",
    //BlockFrostApiKey= "mainnetGk6cqBgfG4nkQtvA1F80hJHfXzYQs8bW",
    BasePath = @"C=\ws\temp\niftylaunchpad\",
    //BasePath= "/home/knut/testnet-node/kc/mintsafe03/",
    PollingIntervalSeconds = 10,
    CollectionId = Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae")
};
builder.Services.AddSingleton(settings);

var address = builder.Configuration["MintsafeApi:BaseAddress"];
if (address == null)
    throw new InvalidDataException("MintsafeApi:BaseAddress config");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(address) });

builder.Services.AddScoped<IAddressUtxoService, AddressUtxoService>();
//builder.Services.AddScoped<IYoloPaymentService, SimplePaymentService>();

await builder.Build().RunAsync();
