using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Mintsafe.Lib.UnitTests;

public class MetadataJsonBuilderShould
{
    private MetadataJsonBuilder _metadataJsonBuilder;

    private static readonly JsonSerializerOptions SerialiserOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public MetadataJsonBuilderShould()
    {
        _metadataJsonBuilder = new MetadataJsonBuilder(
            NullLogger<MetadataJsonBuilder>.Instance,
            FakeGenerator.GenerateSettings());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(15)]
    public void Generate_The_Right_Json_With_Correct_Token_Metadata(int nftCount)
    {
        var collection = FakeGenerator.GenerateCollection();
        var tokens = FakeGenerator.GenerateTokens(nftCount).ToArray();

        var json = _metadataJsonBuilder.GenerateNftStandardJson(
            tokens, collection);

        var deserialised = JsonSerializer.Deserialize<Dictionary<
            string, // 721
            Dictionary<
                string, // PolicyID
                Dictionary<
                    string, // AssetName
                    CnftStandardAsset>>>>(json, SerialiserOptions);

        deserialised.Should().NotBeNull();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var policyAssets = deserialised["721"][collection.PolicyId];
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        policyAssets.Keys.Count.Should().Be(nftCount);
        foreach (var token in tokens)
        {
            var asset = policyAssets[token.AssetName];
            asset.Should().NotBeNull();
            asset.Name.Should().Be(token.Name);
            asset.Description.Should().Be(token.Description);
            asset.Creators.Should().BeEquivalentTo(token.Creators);
            asset.Publishers.Should().BeEquivalentTo(collection.Publishers);
            asset.Image.Should().Be(token.Image);
            asset.MediaType.Should().Be(token.MediaType);
            foreach (var file in token.Files)
            {
                var assetFile = asset.Files.First(f => f.Name == file.Name);
                assetFile.Name.Should().Be(file.Name);
                assetFile.Src.Should().Be(file.Url);
                assetFile.MediaType.Should().Be(file.MediaType);
                assetFile.Hash.Should().Be(file.FileHash);
            }
        }
    }
}
