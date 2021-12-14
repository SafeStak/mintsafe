using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
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
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly HttpClient _httpClient;

    private static readonly MediaTypeHeaderValue CborMediaType = new("application/cbor");
    private static readonly JsonSerializerOptions SerialiserOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BlockfrostClient(
        ILogger<BlockfrostClient> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _httpClient = httpClient;
    }

    public async Task<BlockFrostAddressUtxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
    {
        var relativePath = $"api/v0/addresses/{address}/utxos";

        var isSuccessful = false;
        var responseCode = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync(relativePath, ct).ConfigureAwait(false);
            responseCode = (int)response.StatusCode;
            // Blockfrost returns a 404 for a valid address with zero utxos
            if (responseCode == 404)
            {
                isSuccessful = true;
                return Array.Empty<BlockFrostAddressUtxo>();
            }
            // Other unsuccessful responses
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode, responseBody);
            }
            
            _logger.LogDebug($"{nameof(BlockfrostClient)}.{nameof(GetUtxosAtAddressAsync)} from {relativePath} reponse: {responseCode}");
            var bfResponse = await response.Content.ReadFromJsonAsync<BlockFrostAddressUtxo[]>(SerialiserOptions, ct).ConfigureAwait(false);
            if (bfResponse == null)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"BlockFrost response is null or cannot be deserialised {json}", responseCode, json);
            }
            isSuccessful = true;
            return bfResponse;
        }
        finally
        {
            _instrumentor.TrackDependency(
                EventIds.UtxoRetrievalElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(BlockfrostClient),
                relativePath, 
                nameof(GetUtxosAtAddressAsync),
                isSuccessful: isSuccessful);
        }
    }

    public async Task<BlockFrostTransactionUtxoResponse> GetTransactionAsync(string txHash, CancellationToken ct = default)
    {
        var relativePath = $"api/v0/txs/{txHash}/utxos";

        var isSuccessful = false;
        var responseCode = 0;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync(relativePath, ct).ConfigureAwait(false);
            responseCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode, responseBody);
            }

            _logger.LogDebug($"{nameof(BlockfrostClient)}.{nameof(GetTransactionAsync)} from {relativePath} reponse: {responseCode}");
            var bfResponse = await response.Content.ReadFromJsonAsync<BlockFrostTransactionUtxoResponse>(SerialiserOptions, ct).ConfigureAwait(false);
            if (bfResponse == null)
            {
                var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new BlockfrostResponseException($"BlockFrost response is null or cannot be deserialised {json}", responseCode, json);
            }
            isSuccessful = true;
            return bfResponse;
        }
        finally
        {
            _instrumentor.TrackDependency(
                EventIds.TxInfoRetrievalElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(BlockfrostClient),
                relativePath, nameof(GetTransactionAsync),
                isSuccessful: isSuccessful);
        }
    }

    public async Task<string> SubmitTransactionAsync(byte[] txSignedBinary, CancellationToken ct = default)
    {
        const string relativePath = "api/v0/tx/submit";

        var isSuccessful = false;
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
                throw new BlockfrostResponseException(
                    $"Unsuccessful Blockfrost response:{responseBody}", (int)response.StatusCode, responseBody);
            }

            var txHash = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            isSuccessful = true;
            _logger.LogDebug($"{nameof(BlockfrostClient)}.{nameof(SubmitTransactionAsync)} {relativePath} reponse: {txHash}");
            return txHash;
        }
        finally
        {
            _instrumentor.TrackDependency(
                EventIds.TxSubmissionElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(BlockfrostClient),
                relativePath, nameof(SubmitTransactionAsync),
                isSuccessful: isSuccessful);
        }
    }
}
