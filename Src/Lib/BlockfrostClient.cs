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
        private const string HashedApiKeyMetricName = "Key";

        private readonly HttpClient _httpClient;

        public BlockfrostClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<HttpResponseMessage> GetResponseAsync(
            string queryString, string apiKey, CancellationToken ct = default)
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
