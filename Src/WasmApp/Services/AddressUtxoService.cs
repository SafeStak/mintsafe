using System.Net.Http.Json;
using Mintsafe.Abstractions;

namespace Mintsafe.WasmApp.Services
{
    public interface IAddressUtxoService
    {
        Task<Utxo[]> Get(string address);
    }

    public class AddressUtxoService : IAddressUtxoService
    {
        private readonly HttpClient _httpClient;

        public AddressUtxoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        //TODO use client contract not abstraction?
        public async Task<Utxo[]> Get(string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            var utxos = await _httpClient.GetFromJsonAsync<Utxo[]>($"AddressUtxo/{address}");

            return utxos ?? throw new ArgumentNullException(nameof(utxos));
        }
    }
}
