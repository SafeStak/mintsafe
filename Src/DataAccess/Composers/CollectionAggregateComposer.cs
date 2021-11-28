using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Mapping;

namespace Mintsafe.DataAccess.Composers
{
    public interface ICollectionAggregateComposer
    {
        CollectionAggregate Build(Models.NiftyCollection? collection, IEnumerable<Models.Nifty> nifties, IEnumerable<Models.Sale> sales, IEnumerable<Models.NiftyFile> niftyFiles);
    }

    public class CollectionAggregateComposer : ICollectionAggregateComposer
    {
        private readonly INiftyMapper _niftyMapper;
        private readonly INiftyCollectionMapper _niftyCollectionMapper;
        private readonly ISaleMapper _saleMapper;

        public CollectionAggregateComposer(INiftyMapper niftyMapper, INiftyCollectionMapper niftyCollectionMapper, ISaleMapper saleMapper)
        {
            _niftyMapper = niftyMapper;
            _niftyCollectionMapper = niftyCollectionMapper;
            _saleMapper = saleMapper;
        }

        public CollectionAggregate Build(Models.NiftyCollection? collection, IEnumerable<Models.Nifty> nifties, IEnumerable<Models.Sale> sales, IEnumerable<Models.NiftyFile> niftyFiles)
        {
            var activeSales = sales.Where(IsSaleOpen).ToArray();
            var hydratedNifties = HydrateNifties(nifties, niftyFiles);

            var mappedCollection = _niftyCollectionMapper.Map(collection);
            var mappedSales = activeSales.Select(_saleMapper.Map).ToArray();

            return new CollectionAggregate(mappedCollection, hydratedNifties, mappedSales);
        }

        private static bool IsSaleOpen(Models.Sale sale)
        {
            return sale.IsActive && (!sale.Start.HasValue || !(sale.Start > DateTime.UtcNow)) && (!sale.End.HasValue || !(sale.End < DateTime.UtcNow));
        }

        private Nifty[] HydrateNifties(IEnumerable<Models.Nifty> nifties, IEnumerable<Models.NiftyFile> allFiles)
        {
            var returnNifties = new List<Nifty>();
            foreach (var nifty in nifties)
            {
                var niftyFiles = allFiles.Where(x => x.NiftyId == nifty.RowKey);
                var newNifty = _niftyMapper.Map(nifty, niftyFiles);
                returnNifties.Add(newNifty);
            }

            return returnNifties.ToArray();
        }
    }
}
