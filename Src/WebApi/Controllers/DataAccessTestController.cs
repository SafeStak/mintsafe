using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mintsafe.Abstractions;

namespace Mintsafe.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataAccessTestController : ControllerBase
    {
        private readonly INiftyDataService _dataService;

        public DataAccessTestController(INiftyDataService dataService)
        {
            _dataService = dataService;
        }

        [HttpGet("{collectionId}")]
        public async Task<CollectionAggregate> Get(Guid collectionId, CancellationToken ct)
        {
            var collectionAggregate = await _dataService.GetCollectionAggregateAsync(collectionId, ct);

            return collectionAggregate;
        }

        [HttpPost]
        public async Task<Guid> Post(CancellationToken ct)
        {
            var collectionId = Guid.NewGuid();

            var niftyCollection = new NiftyCollection(collectionId, "a", "name", "desc", true, "", new[] { "a", "b" },
                DateTime.UtcNow, DateTime.UtcNow, 5);

            var niftyId = Guid.NewGuid();

            var niftyFile = new NiftyFile(Guid.NewGuid(), niftyId, "file1.file", "image/jpeg", "http://url.com", "hash");

            var nifty = new Nifty(niftyId, collectionId, true, "file.jpg", "file", "desc", new[] { "a", "b" },
                "http://", "img", new []{ niftyFile }, DateTime.UtcNow, new Royalty(5, "lol"), "v1",
                new List<KeyValuePair<string, string>>()
                {
                    new("a", "b"),
                    new("b", "c")
                });

            var sale = new Sale(
                Guid.NewGuid(), collectionId, true, "Jacob Test", string.Empty, 5, "hash", "hash2", "hash3", 0.1m, 5, 10, DateTime.UtcNow);

            var aggregate = new CollectionAggregate(niftyCollection, new[] {nifty}, new []{sale});

            await _dataService.InsertCollectionAggregateAsync(aggregate, ct);

            return collectionId;
        }
    }
}
