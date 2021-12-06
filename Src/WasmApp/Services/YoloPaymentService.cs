using Mintsafe.Abstractions;
using System.Net.Http.Json;

namespace Mintsafe.WasmApp.Services
{
    public interface IYoloPaymentService
    {
        Task<string> MakePaymentAsync(YoloPayment yoloPayment);
    }

    public class YoloPaymentService : IYoloPaymentService
    {
        private readonly HttpClient _httpClient;

        public YoloPaymentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> MakePaymentAsync(YoloPayment yoloPayment)
        {
            var response = await _httpClient.PostAsJsonAsync($"YoloPayment", yoloPayment);
            var txId = await response.Content.ReadAsStringAsync();
            return txId;
        }
    }
}
