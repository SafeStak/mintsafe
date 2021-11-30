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
        public CollectionAggregate Build(Models.NiftyCollection? collection, IEnumerable<Models.Nifty> nifties, IEnumerable<Models.Sale> sales, IEnumerable<Models.NiftyFile> niftyFiles)
        {
            var activeSales = sales.Where(IsSaleOpen).ToArray();
            var hydratedNifties = HydrateNifties(nifties, niftyFiles);

            var mappedCollection = NiftyCollectionMapper.Map(collection);
            var mappedSales = activeSales.Select(SaleMapper.Map).ToArray();

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
                var newNifty = NiftyMapper.Map(nifty, niftyFiles);
                returnNifties.Add(newNifty);
            }

            return returnNifties.ToArray();
        }
    }
}
