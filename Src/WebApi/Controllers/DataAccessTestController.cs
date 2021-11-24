using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Repositories;

namespace Mintsafe.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataAccessTestController : ControllerBase
    {
        private readonly INiftyDataService _dataService;

        private readonly ISaleRepository _saleRepository;
        private readonly INiftyCollectionRepository _collectionRepository;
        private readonly INiftyRepository _niftyRepository;
        private readonly INiftyFileRepository _niftyFileRepository;

        public DataAccessTestController(INiftyDataService dataService, ISaleRepository saleRepository, INiftyCollectionRepository collectionRepository, INiftyRepository niftyRepository, INiftyFileRepository niftyFileRepository)
        {
            _dataService = dataService;
            _saleRepository = saleRepository;
            _collectionRepository = collectionRepository;
            _niftyRepository = niftyRepository;
            _niftyFileRepository = niftyFileRepository;
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

            var niftyCollection = new NiftyCollection(collectionId, "a", "name", "desc", true, "", new[] {"a", "b"},
                DateTime.UtcNow, DateTime.UtcNow, 5);
            await _collectionRepository.InsertOneAsync(niftyCollection, ct);

            var niftyId = Guid.NewGuid();

            var nifty = new Nifty(niftyId, collectionId, true, "file.jpg", "file", "desc", new[] {"a", "b"},
                "http://", "img", null, DateTime.UtcNow, new Royalty(5, "lol"), "v1",
                new List<KeyValuePair<string, string>>()
                {
                    new("a", "b"),
                    new("b", "c")
                });
            await _niftyRepository.InsertOneAsync(nifty, ct);

            var niftyFile = new NiftyFile(Guid.NewGuid(), niftyId, "file.file", "image/jpeg", "http://url.com", "hash");
            await _niftyFileRepository.InsertOneAsync(niftyFile, ct);

            var sale = new Sale(Guid.NewGuid(), collectionId, true, "Jacob Test", string.Empty, 5, "hash", "hash2", 5, 10);
            await _saleRepository.InsertOneAsync(sale, ct);

            return collectionId;
        }
    }
}
