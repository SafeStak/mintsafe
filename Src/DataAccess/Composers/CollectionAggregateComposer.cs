using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Composers
{
    public interface ICollectionAggregateComposer
    {
        CollectionAggregate Build(NiftyCollection? collection, IEnumerable<Nifty> nifties, IEnumerable<Sale> sales, IEnumerable<NiftyFile> niftyFiles);
    }

    public class CollectionAggregateComposer : ICollectionAggregateComposer
    {
        public CollectionAggregate Build(NiftyCollection? collection, IEnumerable<Nifty> nifties, IEnumerable<Sale> sales, IEnumerable<NiftyFile> niftyFiles)
        {
            var activeSales = sales.Where(IsSaleOpen).ToArray();
            var hydratedNifties = HydrateNifties(nifties, niftyFiles);

            return new CollectionAggregate(collection, hydratedNifties, activeSales);
        }

        private static bool IsSaleOpen(Sale sale)
        {
            return sale.IsActive && (!sale.Start.HasValue || !(sale.Start > DateTime.UtcNow)) && (!sale.End.HasValue || !(sale.End < DateTime.UtcNow));
        }

        //TODO change from record so we can assign files property
        private Nifty[] HydrateNifties(IEnumerable<Nifty> nifties, IEnumerable<NiftyFile> niftyFiles)
        {
            var returnNifties = new List<Nifty>();
            foreach (var nifty in nifties)
            {
                var files = niftyFiles.Where(x => x.NiftyId == nifty.Id).ToArray();
                var newNifty = new Nifty(
                    nifty.Id,
                    nifty.CollectionId,
                    nifty.IsMintable,
                    nifty.AssetName,
                    nifty.Name,
                    nifty.Description,
                    nifty.Creators,
                    nifty.Image,
                    nifty.MediaType,
                    files,
                    nifty.CreatedAt,
                    nifty.Royalty,
                    nifty.Version,
                    nifty.Attributes);
                returnNifties.Add(newNifty);
            }

            return returnNifties.ToArray();
        }
    }
}
