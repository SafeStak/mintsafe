using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib
{
    public class MetadataGenerator : IMetadataGenerator
    {
        private const string MessageStandardKey = "674";
        private const string NftStandardKey = "721";
        private const string NftRoyaltyStandardKey = "777";

        private static readonly JsonSerializerOptions SerialiserOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ILogger<MetadataGenerator> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;

        public class CnftStandardFile
        {
            public string Name { get; set; }
            public string MediaType { get; set; }
            public string Src { get; set; }
        }

        public class CnftStandardAsset
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string MediaType { get; set; }
            public string Image { get; set; }
            public string[] Creators { get; set; }
            public string[] Publishers { get; set; }
            public CnftStandardFile[] Files { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
        }

        public class CnftStandardRoyalty
        {
            public double Pct { get; set; }
            public string[] Addr { get; set; }
        }

        public MetadataGenerator(
            ILogger<MetadataGenerator> logger,
            MintsafeSaleWorkerSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public Task GenerateNftStandardMetadataJsonFile(
            Nifty[] nfts,
            NiftyCollection collection,
            string outputPath,
            CancellationToken ct = default)
        {
            var nftStandard = new Dictionary<
                string, // 721
                Dictionary<
                    string, // PolicyID
                    Dictionary<
                        string, // AssetName
                        CnftStandardAsset>>>();
            var policyCnfts = new Dictionary<
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
                    Creators = nft.Creators,
                    Publishers = collection.Publishers,
                    Files = nft.Files.Select(
                        f => new CnftStandardFile { Name = f.Name, MediaType = f.MediaType, Src = f.Url }).ToArray(),
                    Attributes = nft.Attributes
                };
                nftDictionary.Add(nft.AssetName, nftAsset);
            }
            policyCnfts.Add(collection.PolicyId, nftDictionary);
            nftStandard.Add(NftStandardKey, policyCnfts);

            var json = JsonSerializer.Serialize(nftStandard, SerialiserOptions);

            File.WriteAllText(outputPath, json);

            return Task.CompletedTask;
        }

        public Task GenerateMessageMetadataJsonFile(
            string[] message, 
            string outputPath,
            CancellationToken ct = default)
        {
            var metadataBody = new Dictionary<
                string, // 674
                Dictionary<
                    string, // Msg
                    string[]>>();

            metadataBody.Add(
                MessageStandardKey, 
                new Dictionary<string, string[]>
                    {
                        { "msg", message }
                    });

            var json = JsonSerializer.Serialize(metadataBody, SerialiserOptions);

            File.WriteAllText(outputPath, json);

            return Task.CompletedTask;
        }
    }
}
