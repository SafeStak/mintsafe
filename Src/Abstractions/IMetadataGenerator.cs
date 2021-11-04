﻿using System.Threading;
using System.Threading.Tasks;

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
