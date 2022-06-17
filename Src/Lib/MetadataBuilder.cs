using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public static class MetadataBuilder
{
    public const int MaxMetadataStringLength = 64;
    public const int NftStandardKey = 721;
    public const int MessageStandardKey = 674;
    public const int NftRoyaltyStandardKey = 777;

    public static Dictionary<int, Dictionary<string, object>> BuildNftMintMetadata(
        Nifty[] nifties, NiftyCollection collection)
    {
        var assetNameDictionary = new Dictionary<string, Dictionary<string, object>>();
        foreach (var nft in nifties)
        {
            var metadataDictionary = new Dictionary<string, object>
            {
                { nameof(nft.Name), nft.Name },
            };
            if (!string.IsNullOrWhiteSpace(nft.Description))
            {
                metadataDictionary.Add(
                    nameof(nft.Description),
                    nft.Description.Length > MaxMetadataStringLength
                        ? SplitStringToChunks(nft.Description)
                        : nft.Description);
            }
            if (nft.Creators.Any())
            {
                metadataDictionary.Add(nameof(nft.Creators), nft.Creators);
            }
            if (collection.Publishers.Any())
            {
                metadataDictionary.Add(nameof(collection.Publishers), collection.Publishers);
            }
            if (!string.IsNullOrWhiteSpace(nft.Image))
            {
                metadataDictionary.Add(
                    "image",
                    nft.Image.Length > MaxMetadataStringLength
                        ? SplitStringToChunks(nft.Image)
                        : nft.Image);
            }
            if (!string.IsNullOrWhiteSpace(nft.MediaType))
            {
                metadataDictionary.Add("mediaType", nft.MediaType);
            }
            if (nft.Files.Any())
            {
                foreach (var file in nft.Files)
                {
                    var fileDictionary = new Dictionary<string, object>();
                    if (!string.IsNullOrWhiteSpace(file.Name))
                    {
                        fileDictionary.Add("name", file.Name);
                    }
                    if (!string.IsNullOrWhiteSpace(file.MediaType))
                    {
                        fileDictionary.Add("mediaType", file.MediaType);
                    }
                    if (!string.IsNullOrWhiteSpace(file.FileHash))
                    {
                        fileDictionary.Add(nameof(file.FileHash), file.FileHash);
                    }
                    if (!string.IsNullOrWhiteSpace(file.Src))
                    {
                        fileDictionary.Add(
                            "src",
                            file.Src.Length > MaxMetadataStringLength
                                ? SplitStringToChunks(file.Src)
                                : file.Src);
                    }
                }
            }
            if (nft.Attributes.Length > 1)
            {
                foreach (var attribute in nft.Attributes)
                {
                    metadataDictionary.Add(attribute.Key, attribute.Value);
                }
            }
            assetNameDictionary.Add(nft.AssetName, metadataDictionary);
        }

        var policyMetadata = new Dictionary<string, object>
        {
            { collection.PolicyId, assetNameDictionary }
        };

        var nftStandardMetadata = new Dictionary<int, Dictionary<string, object>>();
        nftStandardMetadata.Add(NftStandardKey, policyMetadata);
        return nftStandardMetadata;
    }

    public static Dictionary<int, Dictionary<string, object>> BuildNftRoyaltyMetadata(Royalty royalty)
    {
        var nftRoyaltyMetadata = new Dictionary<int, Dictionary<string, object>>();
        var royaltyDictionary = new Dictionary<string, object>
        {
            { "rate", royalty.PortionOfSale.ToString() },
            { "addr", royalty.Address.Length > 64 ? SplitStringToChunks(royalty.Address) : royalty.Address }
        };
        nftRoyaltyMetadata.Add(NftRoyaltyStandardKey, royaltyDictionary);
        return nftRoyaltyMetadata;        
    }

    public static Dictionary<int, Dictionary<string, object>> BuildMessageMetadata(string message)
    {
        var messageBodyMetadata = new Dictionary<string, object>
        { 
            { "msg", message.Length > 64 ? SplitStringToChunks(message) : message }, 
            { "at", DateTime.UtcNow.ToString("o") },
        };
        var messageMetadata = new Dictionary<int, Dictionary<string, object>>
            { { MessageStandardKey, messageBodyMetadata } };

        return messageMetadata;
    }

    private static string[] SplitStringToChunks(string? value, int maxLength = MaxMetadataStringLength)
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
}
