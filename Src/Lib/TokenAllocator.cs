using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class TokenAllocator : ITokenAllocator
    {
        private readonly ILogger<TokenAllocator> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly Random _random;

        public TokenAllocator(
            ILogger<TokenAllocator> logger,
            MintsafeSaleWorkerSettings settings)
        {
            _logger = logger;
            _settings = settings;
            _random = new Random();
        }

        public Task<Nifty[]> AllocateTokensForPurchaseAsync(
            PurchaseAttempt request,
            IList<Nifty> saleAllocatedNfts,
            IList<Nifty> saleMintableNfts,
            Sale sale,
            CancellationToken ct = default)
        {
            if (request.NiftyQuantityRequested <= 0)
            {
                throw new ArgumentException("Cannot request zero or negative token allocation", nameof(request));
            }
            if (request.NiftyQuantityRequested > saleMintableNfts.Count)
            {
                throw new CannotAllocateMoreThanMintableException(
                    $"Could not allocate {request.NiftyQuantityRequested} tokens with {saleMintableNfts.Count} mintable nifties", 
                    request.Utxo,
                    sale.Id,
                    request.NiftyQuantityRequested,
                    saleMintableNfts.Count);
            }
            if (request.NiftyQuantityRequested + saleAllocatedNfts.Count > sale.TotalReleaseQuantity)
            {
                throw new CannotAllocateMoreThanSaleReleaseException(
                    "Cannot allocate tokens beyond sale realease quantity", 
                    request.Utxo, 
                    sale.Id,
                    sale.TotalReleaseQuantity,
                    saleAllocatedNfts.Count,
                    request.NiftyQuantityRequested);
            }

            var purchaseAllocated = new List<Nifty>(request.NiftyQuantityRequested);
            while (purchaseAllocated.Count < request.NiftyQuantityRequested)
            {
                var randomIndex = _random.Next(0, saleMintableNfts.Count);
                var tokenAllocated = saleMintableNfts[randomIndex];
                purchaseAllocated.Add(tokenAllocated);
                saleAllocatedNfts.Add(tokenAllocated);
                saleMintableNfts.RemoveAt(randomIndex);
            }

            return Task.FromResult(purchaseAllocated.ToArray());
        }
    }
}
