using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.WebApi.Controllers
{
    public class YoloPayment
    {
        public string? SourceAddress { get; set; }
        public string? DestinationAddress { get; set; }
        public Value[]? Values { get; set; }
        public string[]? Message { get; set; }
        public string? SigningKeyCborHex { get; set; }
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

        [HttpPost]
        public async Task<IActionResult> Post(YoloPayment yoloPayment, CancellationToken ct)
        {
            if (yoloPayment.SourceAddress == null)
                return BadRequest(nameof(yoloPayment.SourceAddress));
            if (yoloPayment.DestinationAddress == null)
                return BadRequest(nameof(yoloPayment.DestinationAddress));
            if (yoloPayment.Values == null)
                return BadRequest(nameof(yoloPayment.Values));
            if (yoloPayment.Message == null)
                return BadRequest(nameof(yoloPayment.Message));
            if (yoloPayment.SigningKeyCborHex == null)
                return BadRequest(nameof(yoloPayment.SigningKeyCborHex));

            var txHash = await _walletService.SendValuesAsync(
                yoloPayment.SourceAddress,
                yoloPayment.DestinationAddress,
                yoloPayment.Values,
                yoloPayment.Message,
                yoloPayment.SigningKeyCborHex,
                ct);

            return Accepted(txHash);
        }
    }
}
