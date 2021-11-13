using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mintsafe.Lib;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.WebApi
{
    public class YoloPayment
    {
        public string SourceAddress { get; set; }
        public string DestinationAddress { get; set; }
        public string[] Message { get; set; }
        public string SigningKeyCborHex { get; set; }
    }
    
    [ApiController]
    [Route("[controller]")]
    public class YoloPaymentController : ControllerBase
    {
        private readonly ILogger<YoloPaymentController> _logger;
        private readonly IYoloWalletService _walletService;

        public YoloPaymentController(
            ILogger<YoloPaymentController> logger,
            IYoloWalletService walletService)
        {
            _logger = logger;
            _walletService = walletService;
        }

        [HttpPost("{address}")]
        public async Task<string> Post(YoloPayment yoloPayment, CancellationToken ct)
        {
            await Task.Delay(50, ct);

            var txHash = "";

            return txHash;
        }
    }
}
