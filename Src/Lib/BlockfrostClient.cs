using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class BlockfrostResponseException : ApplicationException
    {
        public BlockfrostResponseException(string message) : base(message) { }
        public BlockfrostResponseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BlockfrostClient
    {
        private const string TestnetBaseUrl = "https://cardano-testnet.blockfrost.io";
        private const string MainnetBaseUrl = "https://cardano-mainnet.blockfrost.io";
        private readonly HttpClient _httpClient;

        public BlockfrostClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            var relativePath = $"api/v0/addresses/{address}/utxos";

            return Task.FromResult(Array.Empty<Utxo>());
        }

        public Task<Utxo[]> GetTransactionAsync(string txHash, CancellationToken ct = default)
        {
            var relativePath = $"api/v0/txs/{txHash}/utxos";

            return Task.FromResult(Array.Empty<Utxo>());
        }

        public Task<string> SubmitTransactionAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            const string relativePath = "api/v0/tx/submit";
            // Content-Type: application/cbor

            return Task.FromResult("");
        }

        private async Task<HttpResponseMessage> GetAsync(
            string queryString, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            var request = new HttpRequestMessage(HttpMethod.Get, queryString);
            HttpResponseMessage response;
            var isSuccessful = false;

            try
            {
                response = await _httpClient.SendAsync(request, ct);
                if (response.Content == null || !response.IsSuccessStatusCode)
                {
                    throw new BlockfrostResponseException("Failed to retrieve response from Blockfrost.");
                }
                isSuccessful = true;
            }
            catch (Exception ex)
            {
                throw new BlockfrostResponseException("Failed to retrieve response from Blockfrost.", ex);
            }
            finally
            {
                // track dep
            }

            return response;
        }
    }
}
