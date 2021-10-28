using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class BlockfrostResponseException : ApplicationException
    {
        public BlockfrostResponseException(string message) : base(message) { }
        public BlockfrostResponseException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BlockFrostValue
    {
        public string Unit { get; set; }
        public string Quantity { get; set; }
    }

    public class BlockFrostTransactionIo
    {
        public string Address { get; set; }
        public int Output_Index { get; set; }
        public BlockFrostValue[] Amount { get; set; }
    }

    public class BlockFrostTransactionUtxoResponse
    {
        public string Hash { get; set; }
        public BlockFrostTransactionIo[] Inputs { get; set; }
        public BlockFrostTransactionIo[] Outputs { get; set; }
    }

    public class BlockfrostClient
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions SerialiserOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public BlockfrostClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default)
        {
            var relativePath = $"api/v0/addresses/{address}/utxos";

            return Task.FromResult(Array.Empty<Utxo>());
        }

        public async Task<TxBasic> GetTransactionAsync(string txHash, CancellationToken ct = default)
        {
            var relativePath = $"api/v0/txs/{txHash}/utxos";

            var sw = Stopwatch.StartNew();
            var responsePrettyJsonBytes = Array.Empty<byte>();
            try
            {
                var json = await _httpClient.GetStringAsync(relativePath, ct).ConfigureAwait(false);
                Console.WriteLine($"Finished getting JSON response from {relativePath} after {sw.ElapsedMilliseconds}ms");
                var result = JsonSerializer.Deserialize<BlockFrostTransactionUtxoResponse>(json, SerialiserOptions);
                return new TxBasic(
                    result.Hash,
                    result.Inputs.Select(r => new TxIo(r.Address, r.Output_Index, Array.Empty<UtxoValue>())).ToArray(),
                    result.Outputs.Select(r => new TxIo(r.Address, r.Output_Index, Array.Empty<UtxoValue>())).ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting response for {relativePath} after {sw.ElapsedMilliseconds}ms {ex}");
                throw;
            }
        }

        public async Task<string> SubmitTransactionAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            const string relativePath = "api/v0/tx/submit";

            var sw = Stopwatch.StartNew();
            try
            {
                var content = new ByteArrayContent(txSignedBinary);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/cbor");

                //var response = await _httpClient.PostAsync(relativePath, content);
                //var txHash = await response.Content.ReadAsStringAsync();

                //Console.WriteLine($"Finished getting response from {relativePath} after {sw.ElapsedMilliseconds}ms");
                //return txHash;
                await Task.Delay(100);
                return "51e9b6577ad260c273aee5a3786d6b39cce44fc3c49bf44f395499d34b3814f5";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception getting response for {relativePath} after {sw.ElapsedMilliseconds}ms {ex}");
                throw;
            }
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
