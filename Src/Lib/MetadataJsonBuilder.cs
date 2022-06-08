using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    public class CnftOnChainStandardFile
    {
        public string? Name { get; set; }
        public string? MediaType { get; set; }
        public string[]? Src { get; set; }
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
        public IEnumerable<KeyValuePair<string, string>>? Attributes { get; set; }
    }

    public class CnftOnChainStandardAsset
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? MediaType { get; set; }
        public string[]? Image { get; set; }
        public string[]? Creators { get; set; }
        public string[]? Publishers { get; set; }
        public CnftOnChainStandardFile[]? Files { get; set; }
        public IEnumerable<KeyValuePair<string, string>>? Attributes { get; set; }
    }

    public class CnftStandardRoyalty
    {
        public double Rate { get; set; }
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
        private const int MaxMetadataStringLength = 64;

        private static readonly JsonSerializerOptions SerialiserOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ILogger<MetadataJsonBuilder> _logger;
        private readonly IInstrumentor _instrumentor;
        private readonly MintsafeAppSettings _settings;

        public MetadataJsonBuilder(
            ILogger<MetadataJsonBuilder> logger,
            IInstrumentor instrumentor,
            MintsafeAppSettings settings)
        {
            _logger = logger;
            _instrumentor = instrumentor;
            _settings = settings;
        }

        public string GenerateNftStandardJson(
            Nifty[] nfts,
            NiftyCollection collection)
        {
            var hasOnChainNifties = nfts.Any(
                n => (n.Image != null && n.Image.Length > MaxMetadataStringLength)
                || n.Files.Any(nf => nf.Url.Length > MaxMetadataStringLength));
            
            return hasOnChainNifties
                ? GetOnChainNftStandardJson(nfts, collection)
                : GetOffChainNftStandardJson(nfts, collection);            
        }

        private string GetOffChainNftStandardJson(Nifty[] nfts, NiftyCollection collection)
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
                    Files = nft.Files.Length == 0 ? null // don't serialise empty arrays
                        : nft.Files.Select(
                            f => new CnftStandardFile { Name = f.Name, MediaType = f.MediaType, Src = f.Url, Hash = f.FileHash }).ToArray(),
                    Attributes = nft.Attributes.Length == 0 ? null : nft.Attributes
                };
                nftDictionary.Add(nft.AssetName, nftAsset);
            }
            policyCnfts.Add(collection.PolicyId, nftDictionary);
            nftStandard.Add(NftStandardKey, policyCnfts);

            var json = JsonSerializer.Serialize(nftStandard, SerialiserOptions);
            _logger.LogDebug($"NFT Metadata JSON (off-chain) built after {sw.ElapsedMilliseconds}ms");

            return json;
        }

        private string GetOnChainNftStandardJson(
            Nifty[] nfts,
            NiftyCollection collection)
        {
            var nftStandard = new Dictionary<
                string, // 721
                Dictionary<
                    string, // PolicyID
                    Dictionary<
                        string, // AssetName
                        CnftOnChainStandardAsset>>>();
            var policyCnfts = new Dictionary<
                string, // PolicyID
                Dictionary<
                    string, // AssetName
                    CnftOnChainStandardAsset>>();

            var sw = Stopwatch.StartNew();
            var nftDictionary = new Dictionary<string, CnftOnChainStandardAsset>();
            foreach (var nft in nfts)
            {
                var nftAsset = new CnftOnChainStandardAsset
                {
                    Name = nft.Name,
                    Description = nft.Description,
                    Image = SplitStringToChunks(nft.Image),
                    MediaType = nft.MediaType,
                    Creators = nft.Creators,
                    Publishers = collection.Publishers,
                    Files = nft.Files.Length == 0 ? null // don't serialise empty arrays
                        : nft.Files.Select(
                            f => new CnftOnChainStandardFile { Name = f.Name, MediaType = f.MediaType, Src = SplitStringToChunks(f.Url), Hash = f.FileHash }).ToArray(),
                    Attributes = nft.Attributes.Length == 0 ? null : nft.Attributes
                };
                nftDictionary.Add(nft.AssetName, nftAsset);
            }
            policyCnfts.Add(collection.PolicyId, nftDictionary);
            nftStandard.Add(NftStandardKey, policyCnfts);

            var json = JsonSerializer.Serialize(nftStandard, SerialiserOptions);
            _logger.LogDebug($"NFT Metadata JSON (on-chain) built after {sw.ElapsedMilliseconds}ms");

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
            _logger.LogDebug($"Message Metadata JSON built after {sw.ElapsedMilliseconds}ms");

            return json;
        }

        public string GenerateRoyaltyJson(Royalty royalty)
        {
            var sw = Stopwatch.StartNew();
            var metadataBody = new Dictionary<
                string, // 777
                CnftStandardRoyalty>
            {
                {
                    NftRoyaltyStandardKey,
                    new CnftStandardRoyalty { Rate = royalty.PortionOfSale, Addr = SplitStringToChunks(royalty.Address) }
                }
            };
            var json = JsonSerializer.Serialize(metadataBody, SerialiserOptions);
            _logger.LogDebug($"Royalty Metadata JSON built after {sw.ElapsedMilliseconds}ms");

            return json;
        }

        public static string[] SplitStringToChunks(string? value, int maxLength = MaxMetadataStringLength)
        {
            if (value == null) 
                return Array.Empty<string>();
            if (value.Length <= maxLength)
                return new[] { value };

            var offsetLength = maxLength - 1;
            var itemsLength = (value.Length + offsetLength) / maxLength;
            var items = new string[itemsLength];
            for (var i = 0; i < itemsLength; i++)
            {
                var substringStartIndex = i * maxLength;
                var substringLength = (substringStartIndex + maxLength) <= value.Length 
                    ? maxLength 
                    : value.Length % maxLength; // take remainder mod to prevent index-out-of-bounds
                var segment = value.Substring(substringStartIndex, substringLength);
                items[i] = segment;
            }
            return items;
        }

        public static string GetBase64SvgForMessage(string message, string title = "")
        {
            const int MaxMessageBodyLength = 256;
            const int MaxMessageLineCharLength = 32;
            if (title.Length > MaxMessageLineCharLength)
                throw new ArgumentException($"{nameof(title)} cannot be greater than {MaxMessageLineCharLength} characters", nameof(title));
            if (message.Length > MaxMessageBodyLength)
                throw new ArgumentException($"{nameof(message)} cannot be greater than {MaxMessageBodyLength} characters", nameof(message));

            var svgBuilder = new StringBuilder($"<svg width='300' height='300' viewBox='0 0 300 300' xmlns='http://www.w3.org/2000/svg'><rect x='0' y='0' height='300' width='300' fill='#EBF3F3' stroke='#AA828E' stroke-width='10'/><g text-anchor='start' style='font: bold 270% monospace;' fill='#AA828E'>");
            if (!string.IsNullOrWhiteSpace(title))
            {
                svgBuilder.Append($"<text text-anchor='middle' font-size='large' x='50%' y='15%'>{title}</text>");
            }

            var chunks = SplitStringToChunks(message, MaxMessageLineCharLength);
            var yOffset = 35;
            foreach (var chunk in chunks)
            {
                svgBuilder.Append($"<text x='3%' y='{yOffset}%' font-size='large'>{chunk}</text>");
                yOffset += 15;
            }
            svgBuilder.Append($"<text text-anchor='end' x='97%' y='95%' font-size='small'>reply @ mintsafe.io</text>");
            svgBuilder.Append("</g></svg>");

            var base64Svg = Convert.ToBase64String(Encoding.UTF8.GetBytes(svgBuilder.ToString()));

            return $"data:image/svg+xml;base64,{base64Svg}";
        }
    }
}
