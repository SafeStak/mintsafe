using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class MetadataFileGenerator : IMetadataFileGenerator
{
    private readonly ILogger<MetadataFileGenerator> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly IMetadataJsonBuilder _metadataJsonBuilder;

    public MetadataFileGenerator(
        ILogger<MetadataFileGenerator> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings,
        IMetadataJsonBuilder metadataJsonBuilder)
    {
        _logger = logger;
        _instrumentor = instrumentor;
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
        _instrumentor.TrackDependency(
            EventIds.MetadataFileElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(MetadataFileGenerator),
            outputPath,
            nameof(GenerateNftStandardMetadataJsonFile),
            isSuccessful: true,
            customProperties: new Dictionary<string, object>
                {
                    { "NftCount", nfts.Length },
                    { "JsonLength", json.Length }
                });
        _logger.LogDebug($"NFT Metadata JSON file generated at {outputPath} after {sw.ElapsedMilliseconds}ms");
    }

    public async Task GenerateMessageMetadataJsonFile(
        string[] message,
        string outputPath,
        CancellationToken ct = default)
    {
        var json = _metadataJsonBuilder.GenerateMessageJson(message);
        var sw = Stopwatch.StartNew();
        await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);
        _instrumentor.TrackDependency(
            EventIds.MetadataFileElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(MetadataFileGenerator),
            outputPath,
            nameof(GenerateMessageMetadataJsonFile),
            isSuccessful: true,
            customProperties: new Dictionary<string, object>
                {
                    { "JsonLength", json.Length }
                });
        _logger.LogDebug($"Message Metadata JSON file generated at {outputPath} after {sw.ElapsedMilliseconds}ms");
    }
}
