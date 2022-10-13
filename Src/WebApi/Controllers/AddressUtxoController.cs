using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AddressUtxoController : ControllerBase
    {
        private readonly ILogger<AddressUtxoController> _logger;
        private readonly IUtxoRetriever _utxoRetriever;

        public AddressUtxoController(
            ILogger<AddressUtxoController> logger,
            IUtxoRetriever utxoRetriever)
        {
            _logger = logger;
            _utxoRetriever = utxoRetriever;
        }

        [HttpGet("{address}")]
        public async Task<UnspentTransactionOutput[]> Get(string address, CancellationToken ct)
        {
            var utxos = await _utxoRetriever.GetUtxosAtAddressAsync(address, ct);

            return utxos;
        }
    }
}
