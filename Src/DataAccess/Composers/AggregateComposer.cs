using Mintsafe.Abstractions;
using Mintsafe.DataAccess.Mappers;

namespace Mintsafe.DataAccess.Composers;

public interface IAggregateComposer
{
    ProjectAggregate Build(
        Models.NiftyCollection collection, 
        IEnumerable<Models.Nifty> nifties, 
        IEnumerable<Models.Sale> sales, 
        IEnumerable<Models.NiftyFile> niftyFiles);

    SaleAggregate? BuildSaleAggregate(
        Models.Sale sale,
        Models.NiftyCollection collection,
        IEnumerable<Models.Nifty> nifties,
        IEnumerable<Models.NiftyFile> niftyFiles);
}

public class AggregateComposer : IAggregateComposer
{
    public ProjectAggregate Build(
        Models.NiftyCollection collection, 
        IEnumerable<Models.Nifty> nifties, 
        IEnumerable<Models.Sale> sales,
        IEnumerable<Models.NiftyFile> niftyFiles)
    {
        var activeSales = sales.Where(IsSaleOpen).ToArray();
        var hydratedNifties = HydrateNifties(nifties, niftyFiles);
        var mappedCollection = NiftyCollectionMapper.Map(collection);
        var mappedSales = activeSales.Select(SaleMapper.Map).ToArray();

        return new ProjectAggregate(mappedCollection, hydratedNifties, mappedSales);
    }

    public SaleAggregate? BuildSaleAggregate(
        Models.Sale sale, 
        Models.NiftyCollection collection, 
        IEnumerable<Models.Nifty> nifties, 
        IEnumerable<Models.NiftyFile> niftyFiles)
    {
        if (!IsSaleOpen(sale))
            return null;

        var mappedSale = SaleMapper.Map(sale);
        var mappedCollection = NiftyCollectionMapper.Map(collection);
        var hydratedNifties = HydrateNifties(nifties, niftyFiles);

        return new SaleAggregate(mappedSale, mappedCollection, hydratedNifties);
    }

    private static bool IsSaleOpen(Models.Sale sale)
    {
        return sale.IsActive
            && (sale.Start <= DateTime.UtcNow)
            && (!sale.End.HasValue || (sale.End.HasValue && sale.End > DateTime.UtcNow));
    }

    private static Nifty[] HydrateNifties(IEnumerable<Models.Nifty> nifties, IEnumerable<Models.NiftyFile> allFiles)
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
