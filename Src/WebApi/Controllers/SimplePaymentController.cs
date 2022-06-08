using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SimplePaymentController : ControllerBase
    {
        private readonly ILogger<SimplePaymentController> _logger;
        private readonly ISimpleWalletService _walletService;

        public SimplePaymentController(
            ILogger<SimplePaymentController> logger,
            ISimpleWalletService walletService)
        {
            _logger = logger;
            _walletService = walletService;
        }

        //[HttpPost]
        //public async Task<IActionResult> Post(YoloPayment yoloPayment, CancellationToken ct)
        //{
        //    if (yoloPayment.SourceAddress == null)
        //        return BadRequest(nameof(yoloPayment.SourceAddress));
        //    if (yoloPayment.DestinationAddress == null)
        //        return BadRequest(nameof(yoloPayment.DestinationAddress));
        //    if (yoloPayment.Values == null)
        //        return BadRequest(nameof(yoloPayment.Values));
        //    if (yoloPayment.Message == null)
        //        return BadRequest(nameof(yoloPayment.Message));
        //    if (yoloPayment.SigningKeyCborHex == null)
        //        return BadRequest(nameof(yoloPayment.SigningKeyCborHex));

        //    var txHash = await _walletService.SendValuesAsync(
        //        yoloPayment.SourceAddress,
        //        yoloPayment.DestinationAddress,
        //        yoloPayment.Values,
        //        yoloPayment.Message,
        //        yoloPayment.SigningKeyCborHex,
        //        ct);

        //    return Accepted(txHash);
        //}
    }
}
