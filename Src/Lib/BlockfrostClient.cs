using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class BlockfrostClient : IBlockfrostClient
    {
        private readonly ILogger<BlockfrostClient> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly HttpClient _httpClient;

        private static readonly MediaTypeHeaderValue CborMediaType = new("application/cbor");
        private static readonly JsonSerializerOptions SerialiserOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public BlockfrostClient(
            ILogger<BlockfrostClient> logger,
            MintsafeSaleWorkerSettings settings,
            HttpClient httpClient)
        {
            _logger = logger;
            _settings = settings;
            _httpClient = httpClient;
        }

        public Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            var relativePath = $"api/v0/addresses/{address}/utxos";

            return Task.FromResult(Array.Empty<Utxo>());
        }

        public async Task<BlockFrostTransactionUtxoResponse> GetTransactionAsync(string txHash, CancellationToken ct = default)
        {
            var relativePath = $"api/v0/txs/{txHash}/utxos";

            var responseCode = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await _httpClient.GetAsync(relativePath, ct).ConfigureAwait(false);
                responseCode = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(ct);
                    throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode);
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                _logger.LogDebug($"GetTransactionAsync {relativePath} reponse: {json}");

                return JsonSerializer.Deserialize<BlockFrostTransactionUtxoResponse>(json, SerialiserOptions);
            }
            finally
            {
                _logger.LogInformation($"Finished getting response ({responseCode}) from {relativePath} after {sw.ElapsedMilliseconds}ms");
            }
        }

        public async Task<string> SubmitTransactionAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            const string relativePath = "api/v0/tx/submit";

            var responseCode = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                var content = new ByteArrayContent(txSignedBinary);
                content.Headers.ContentType = CborMediaType;

                var response = await _httpClient.PostAsync(relativePath, content, ct);
                responseCode = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(ct);
                    throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode);
                }

                var txHash = await response.Content.ReadAsStringAsync(ct);
                _logger.LogDebug($"SubmitTransactionAsync {relativePath} reponse: {txHash}");
                return txHash;
            }
            finally
            {
                _logger.LogInformation($"Finished getting response ({responseCode}) from {relativePath} after {sw.ElapsedMilliseconds}ms");
            }
        }
    }
}
