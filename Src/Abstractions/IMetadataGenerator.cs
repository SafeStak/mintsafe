using System.Threading;
using System.Threading.Tasks;

public interface IMetadataGenerator
{
    Task GenerateMetadataJsonFile(
        Nifty[] nfts,
        NiftyCollection collection,
        string path,
        CancellationToken ct = default);
}
