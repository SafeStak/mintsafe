﻿using System;
using System.Collections.Generic;

namespace NiftyLaunchpad.Lib
{
    public record CollectionAggregate(
        NiftyCollection Collection,
        Nifty[] Tokens,
        NiftySale[] ActiveSales);

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
        Guid CollectionId,
        bool IsMintable,
        string AssetName,
        string Name,
        string Description,
        string[] Artists,
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
        string Url);

    public record Royalty(
        double PortionOfSale,
        string Address);

    public record NiftySale(
        Guid Id,
        Guid CollectionId,
        bool IsActive,
        string Name,
        string Description,
        long LovelacesPerToken,
        string SaleAddress,
        int TotalReleaseQuantity,
        int MaxAllowedPurchaseQuantity,
        DateTime? Start = null,
        DateTime? End = null);

    public record NiftySalePurchaseRequest(
        Guid Id,
        Guid SaleId,
        Utxo Utxo,
        int NiftyQuantityRequested,
        long ChangeInLovelace);
}