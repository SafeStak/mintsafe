using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class MetadataGenerator : IMetadataGenerator
    {
        public class NftStandardAsset
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string MediaType { get; set; }
            public string Image { get; set; }
            public string[] Creators { get; set; }
        }

        public Task GenerateMetadataJsonFile(
            Nifty[] nfts,
            NiftyCollection collection,
            string path,
            CancellationToken ct = default)
        {
            var nftStandard = new Dictionary<
                string, // 721
                Dictionary<
                    string, // PolicyID
                    Dictionary<
                        string, // AssetName
                        NftStandardAsset>>>();
            var policy = new Dictionary<
                string, // PolicyID
                Dictionary<
                    string, // AssetName
                    NftStandardAsset>>();

            var nftDictionary = new Dictionary<string, NftStandardAsset>();
            foreach (var nft in nfts)
            {
                var nftAsset = new NftStandardAsset
                {
                    Name = nft.Name,
                    Description = nft.Description,
                    Image = nft.Image,
                    MediaType = nft.MediaType,
                    Creators = nft.Artists
                };
                nftDictionary.Add(nft.AssetName, nftAsset);
            }
            policy.Add(collection.PolicyId, nftDictionary);
            nftStandard.Add("721", policy);

            var json = JsonSerializer.Serialize(nftStandard);

            File.WriteAllText(path, json);

            return Task.CompletedTask;
        }
    }
}
