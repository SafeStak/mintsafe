using System;
using System.Collections.Generic;

namespace Mintsafe.Abstractions
{
    public record CollectionAggregate(
        NiftyCollection Collection,
        Nifty[] Tokens,
        Sale[] ActiveSales);

    public record NiftyCollection(
        Guid Id,
        string PolicyId,
        string Name,
        string Description,
        bool IsActive,
        string BrandImage,
        string[] Publishers,
        DateTime CreatedAt,
        DateTime LockedAt,
        long SlotExpiry);

    public record Nifty(
        Guid Id,
        Guid CollectionId,
        bool IsMintable,
        string AssetName,
        string Name,
        string Description,
        string[] Creators,
        string Image,
        string MediaType,
        NiftyFile[] Files,
        DateTime CreatedAt,
        Royalty Royalty,
        string Version,
        Dictionary<string, string> Attributes);

    public record NiftyFile(
        Guid Id,
        string Name,
        string MediaType,
        string Url,
        string FileHash = "");

    public record Royalty(
        double PortionOfSale,
        string Address);

    public record Sale(
        Guid Id,
        Guid CollectionId,
        bool IsActive,
        string Name,
        string Description,
        long LovelacesPerToken,
        string SaleAddress,
        string ProceedsAddress,
        int TotalReleaseQuantity,
        int MaxAllowedPurchaseQuantity,
        DateTime? Start = null,
        DateTime? End = null);

    public record PurchaseAttempt(
        Guid Id,
        Guid SaleId,
        Utxo Utxo,
        int NiftyQuantityRequested,
        long ChangeInLovelace);
}