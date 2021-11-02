using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TokenAllocator : ITokenAllocator
    {
        private readonly NiftyLaunchpadSettings _settings;
        private readonly List<Nifty> _mintableNfts;
        private Random _random;

        public TokenAllocator(
            NiftyLaunchpadSettings settings,
            List<Nifty> mintableNfts)
        {
            _settings = settings;
            _mintableNfts = mintableNfts;
            _random = new Random();
        }

        public Task<Nifty[]> AllocateTokensAsync(
            NiftySalePurchaseRequest request,
            List<Nifty> allocatedTokens,
            NiftySale sale,
            CancellationToken ct = default)
        {
            if (request.NiftyQuantityRequested <= 0)
            {
                throw new ArgumentException("Cannot request zero or negative token allocation", nameof(request));
            }
            if (request.NiftyQuantityRequested > _mintableNfts.Count)
            {
                throw new NoMintableTokensLeftException(
                    "No more mintable tokens in collection", request.Utxo, request.NiftyQuantityRequested);
            }

            if (request.NiftyQuantityRequested + allocatedTokens.Count > sale.TotalReleaseQuantity)
            {
                throw new SaleReleaseQuantityExceededException(
                    "Cannot allocate tokens past sale realease quantity", request.Utxo, request.NiftyQuantityRequested);
            }

            var allocatedNfts = new List<Nifty>(request.NiftyQuantityRequested);
            while (allocatedNfts.Count < request.NiftyQuantityRequested)
            {
                var randomIndex = _random.Next(0, _mintableNfts.Count);
                var tokenAllocated = _mintableNfts[randomIndex];
                allocatedNfts.Add(tokenAllocated);
                _mintableNfts.RemoveAt(randomIndex);
            }

            return Task.FromResult(allocatedNfts.ToArray());
        }
    }
}
