using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IMetadataGenerator
{
    Task GenerateNftStandardMetadataJsonFile(
        Nifty[] nfts,
        NiftyCollection collection,
        string outputPath,
        CancellationToken ct = default);

    Task GenerateMessageMetadataJsonFile(
        string[] message,
        string outputPath,
        CancellationToken ct = default);
}
