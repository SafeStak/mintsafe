using System;
using System.Collections.Generic;

namespace NiftyLaunchpad.Lib
{
    public record NiftyCollection(
        Guid Id,
        string PolicyId,
        string Name,
        string Description,
        bool IsActive,
        int ValidFromSlot,
        int ValidToSlot,
        string[] Publishers,
        string PreviewImage,
        string CreatedBy,
        DateTime CreatedAt);

    public record Nifty(
        Guid Id,
        string PolicyId,
        string AssetName,
        string AssetId,
        string Name,
        string Description,
        string MediaType,
        string[] Publishers,
        string Image,
        string Files,
        bool IsMintable,
        string CreatedBy,
        DateTime CreatedAt,
        Dictionary<string, string> Attributes);

    public record CollectionAggregate(
        NiftyCollection Collection,
        Nifty[] Tokens);
}
