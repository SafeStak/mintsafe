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
            CancellationToken ct = default)
        {
            if (request.NiftyQuantityRequested <= 0)
            {
                throw new ArgumentException("Cannot request zero or negative token allocation", nameof(request));
            }
            if (request.NiftyQuantityRequested > _mintableNfts.Count)
            {
                throw new AllMintableTokensForSaleAllocated(
                    "All tokens already allocated", request.Utxo, request.NiftyQuantityRequested);
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
