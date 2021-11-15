using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class BlockfrostClient : IBlockfrostClient
{
    private readonly ILogger<BlockfrostClient> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly HttpClient _httpClient;

    private static readonly MediaTypeHeaderValue CborMediaType = new("application/cbor");
    private static readonly JsonSerializerOptions SerialiserOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BlockfrostClient(
        ILogger<BlockfrostClient> logger,
        MintsafeAppSettings settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings;
        _httpClient = httpClient;
    }

    public async Task<BlockFrostAddressUtxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
    {
        var relativePath = $"api/v0/addresses/{address}/utxos";

        var responseCode = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync(relativePath, ct).ConfigureAwait(false);
            responseCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode);
            }

            _logger.LogDebug($"{nameof(BlockfrostClient)}.{nameof(GetUtxosAtAddressAsync)} from {relativePath} reponse: {responseCode}");
            var bfResponse = await response.Content.ReadFromJsonAsync<BlockFrostAddressUtxo[]>(SerialiserOptions, ct).ConfigureAwait(false);
            if (bfResponse == null)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"BlockFrost response is null or cannot be deserialised {json}", responseCode);
            }

            return bfResponse;
        }
        finally
        {
            _logger.LogInformation($"Finished getting response ({responseCode}) from {relativePath} after {sw.ElapsedMilliseconds}ms");
        }
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
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode);
            }

            _logger.LogDebug($"{nameof(BlockfrostClient)}.{nameof(GetTransactionAsync)} from {relativePath} reponse: {responseCode}");
            var bfResponse = await response.Content.ReadFromJsonAsync<BlockFrostTransactionUtxoResponse>(SerialiserOptions, ct).ConfigureAwait(false);
            if (bfResponse == null)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"BlockFrost response is null or cannot be deserialised {json}", responseCode);
            }

            return bfResponse;
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

            var response = await _httpClient.PostAsync(relativePath, content, ct).ConfigureAwait(false);
            responseCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode);
            }

            var txHash = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogDebug($"{nameof(BlockfrostClient)}.{nameof(SubmitTransactionAsync)} {relativePath} reponse: {txHash}");
            return txHash;
        }
        finally
        {
            _logger.LogInformation($"Finished getting response ({responseCode}) from {relativePath} after {sw.ElapsedMilliseconds}ms");
        }
    }
}
