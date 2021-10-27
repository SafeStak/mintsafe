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
        string BrandImage,
        string[] Publishers,
        DateTime CreatedAt,
        DateTime LockedAt);

    public record Nifty(
        Guid Id,
        string PolicyId,
        bool IsMintable,
        string AssetName,
        string Name,
        string Description,
        string[] Artists,
        string Image,
        string MediaType,
        NiftyFile[] Files,
        DateTime CreatedAt,
        Dictionary<string, string> Attributes);

    public record NiftyFile(Guid Id, string Name, string Url);

    public record CollectionAggregate(NiftyCollection Collection, Nifty[] Tokens);
}
