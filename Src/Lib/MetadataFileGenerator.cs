using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class MetadataFileGenerator : IMetadataFileGenerator
{
    private readonly ILogger<MetadataFileGenerator> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly IMetadataJsonBuilder _metadataJsonBuilder;

    public MetadataFileGenerator(
        ILogger<MetadataFileGenerator> logger,
        MintsafeAppSettings settings,
        IMetadataJsonBuilder metadataJsonBuilder)
    {
        _logger = logger;
        _settings = settings;
        _metadataJsonBuilder = metadataJsonBuilder;
    }

    public async Task GenerateNftStandardMetadataJsonFile(
        Nifty[] nfts,
        NiftyCollection collection,
        string outputPath,
        CancellationToken ct = default)
    {
        var json = _metadataJsonBuilder.GenerateNftStandardJson(nfts, collection);
        var sw = Stopwatch.StartNew();
        await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);
        _logger.LogInformation($"NFT Metadata JSON file generated at {outputPath} after {sw.ElapsedMilliseconds}ms");
    }

    public async Task GenerateMessageMetadataJsonFile(
        string[] message,
        string outputPath,
        CancellationToken ct = default)
    {
        var json = _metadataJsonBuilder.GenerateMessageJson(message);
        var sw = Stopwatch.StartNew();
        await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);
        _logger.LogInformation($"Message Metadata JSON file generated at {outputPath} after {sw.ElapsedMilliseconds}ms");
    }
}
