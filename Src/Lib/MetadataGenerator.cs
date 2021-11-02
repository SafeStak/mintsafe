﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class MetadataGenerator : IMetadataGenerator
    {
        private const string NftStandardKey = "721";
        private const string NftRoyaltyStandardKey = "777";

        private static readonly JsonSerializerOptions SerialiserOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public class CnftStandardFile
        {
            public string Name { get; set; }
            public string MediaType { get; set; }
            public string Url { get; set; }
        }

        public class CnftStandardAsset
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string MediaType { get; set; }
            public string Image { get; set; }
            public string[] Creators { get; set; }
            public CnftStandardFile[] Files { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
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
                        CnftStandardAsset>>>();
            var policy = new Dictionary<
                string, // PolicyID
                Dictionary<
                    string, // AssetName
                    CnftStandardAsset>>();

            var nftDictionary = new Dictionary<string, CnftStandardAsset>();
            foreach (var nft in nfts)
            {
                var nftAsset = new CnftStandardAsset
                {
                    Name = nft.Name,
                    Description = nft.Description,
                    Image = nft.Image,
                    MediaType = nft.MediaType,
                    Creators = nft.Artists,
                    Files = nft.Files.Select(
                        f => new CnftStandardFile { Name = f.Name, MediaType = f.MediaType, Url = f.Url }).ToArray(),
                    Attributes = nft.Attributes
                };
                nftDictionary.Add(nft.AssetName, nftAsset);
            }
            policy.Add(collection.PolicyId, nftDictionary);
            nftStandard.Add(NftStandardKey, policy);

            var json = JsonSerializer.Serialize(nftStandard, SerialiserOptions);

            File.WriteAllText(path, json);

            return Task.CompletedTask;
        }
    }
}
