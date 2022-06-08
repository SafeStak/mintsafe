using Mintsafe.Abstractions;
using System.Net.Http.Json;

namespace Mintsafe.WasmApp.Services;

public record SimplePayment
{
    public string SourcePaymentAddress { get; init; }
    public string SourcePaymentAddressSigningKey { get; init; }
    public string DestinationPaymentAddress { get; init; }
    public ulong PaymentLovelaces { get; init; }
    public string Comment { get; init; }
}

public interface ISimplePaymentService
{
    Task<string> MakePaymentAsync(SimplePayment yoloPayment);
}

public class SimplePaymentService : ISimplePaymentService
{
    private readonly HttpClient _httpClient;

    public SimplePaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> MakePaymentAsync(SimplePayment simplePayment)
    {
        var response = await _httpClient.PostAsJsonAsync($"SimplePayment", simplePayment);
        var txId = await response.Content.ReadAsStringAsync();
        return txId;
    }
}
