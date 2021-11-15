using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace Mintsafe.Lib
{
    #region Cardano NFT metadata types
    public class CnftStandardFile
    {
        public string? Name { get; set; }
        public string? MediaType { get; set; }
        public string? Src { get; set; }
        public string? Hash { get; set; }
    }

    public class CnftStandardAsset
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MediaType { get; set; }
        public string? Image { get; set; }
        public string[]? Creators { get; set; }
        public string[]? Publishers { get; set; }
        public CnftStandardFile[]? Files { get; set; }
        public Dictionary<string, string>? Attributes { get; set; }
    }

    public class CnftStandardRoyalty
    {
        public double Pct { get; set; }
        public string[]? Addr { get; set; }
    }
    #endregion

    public interface IMetadataJsonBuilder
    {
        string GenerateMessageJson(string[] message);
        string GenerateNftStandardJson(Nifty[] nfts, NiftyCollection collection);
    }

    public class MetadataJsonBuilder : IMetadataJsonBuilder
    {
        private const string MessageStandardKey = "674";
        private const string NftStandardKey = "721";
        private const string NftRoyaltyStandardKey = "777";

        private static readonly JsonSerializerOptions SerialiserOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ILogger<MetadataJsonBuilder> _logger;
        private readonly MintsafeAppSettings _settings;

        public MetadataJsonBuilder(
            ILogger<MetadataJsonBuilder> logger,
            MintsafeAppSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public string GenerateNftStandardJson(
            Nifty[] nfts,
            NiftyCollection collection)
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

            var sw = Stopwatch.StartNew();
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
                        f => new CnftStandardFile { Name = f.Name, MediaType = f.MediaType, Src = f.Url, Hash = f.FileHash }).ToArray(),
                    Attributes = nft.Attributes
                };
                nftDictionary.Add(nft.AssetName, nftAsset);
            }
            policyCnfts.Add(collection.PolicyId, nftDictionary);
            nftStandard.Add(NftStandardKey, policyCnfts);

            var json = JsonSerializer.Serialize(nftStandard, SerialiserOptions);
            _logger.LogInformation($"NFT Metadata JSON built after {sw.ElapsedMilliseconds}ms");

            return json;
        }

        public string GenerateMessageJson(string[] message)
        {
            var sw = Stopwatch.StartNew();
            var metadataBody = new Dictionary<
                string, // 674
                Dictionary<
                    string, // Msg
                    string[]>>
            {
                {
                    MessageStandardKey,
                    new Dictionary<string, string[]>
                    {
                        { "msg", message }
                    }
                }
            };

            var json = JsonSerializer.Serialize(metadataBody, SerialiserOptions);
            _logger.LogInformation($"Message Metadata JSON built after {sw.ElapsedMilliseconds}ms");

            return json;
        }
    }
}
