using FluentAssertions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static NiftyLaunchpad.Lib.MetadataGenerator;

namespace NiftyLaunchpad.Lib.UnitTests
{
    public class MetadataGeneratorShould
    {
        private MetadataGenerator _metadataGenerator;

        public MetadataGeneratorShould()
        {
            _metadataGenerator = new MetadataGenerator();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Generate_The_Right_Json_With_Correct_Token_Metadata(int nftCount)
        {
            var collection = Generator.GenerateCollection();
            var tokens = Generator.GenerateTokens(nftCount).ToArray();
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
            deserialised["721"][collection.PolicyId].Keys.Count.Should().Be(nftCount);
        }

        
    }
}
