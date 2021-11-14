﻿using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Mintsafe.Lib.MetadataGenerator;

namespace Mintsafe.Lib.UnitTests
{
    public class MetadataGeneratorShould
    {
        private MetadataGenerator _metadataGenerator;

        public MetadataGeneratorShould()
        {
            _metadataGenerator = new MetadataGenerator(
                NullLogger<MetadataGenerator>.Instance, 
                FakeGenerator.GenerateSettings());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Generate_The_Right_Json_With_Correct_Token_Metadata(int nftCount)
        {
            var collection = FakeGenerator.GenerateCollection();
            var tokens = FakeGenerator.GenerateTokens(nftCount).ToArray();
            var fileName = $"metadata{nftCount}.json";

            await _metadataGenerator.GenerateNftStandardMetadataJsonFile(
                tokens, collection, fileName);

            var json = File.ReadAllText(fileName);
            var deserialised = JsonSerializer.Deserialize<Dictionary<
                string, // 721
                Dictionary<
                    string, // PolicyID
                    Dictionary<
                        string, // AssetName
                        CnftStandardAsset>>>>(json);
            
            deserialised.Should().NotBeNull();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            deserialised["721"][collection.PolicyId].Keys.Count.Should().Be(nftCount);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        
    }
}
