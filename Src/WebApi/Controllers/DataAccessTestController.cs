using System;
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

        public DataAccessTestController(INiftyDataService dataService, ISaleRepository saleRepository)
        {
            _dataService = dataService;
            _saleRepository = saleRepository;
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
            var guid = Guid.NewGuid();
            var sale = new Sale(guid, Guid.NewGuid(), true, "Jacob Test", string.Empty, 5, "hash", "hash2", 5, 10);
            await _saleRepository.InsertOneAsync(sale, ct);

            return guid;
        }
    }
}
