using System;
using System.Collections.Generic;

namespace Mintsafe.Abstractions;

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
    KeyValuePair<string, string>[] Attributes);

public record NiftyFile(
    Guid Id,
    Guid NiftyId,
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
    string CreatorAddress,
    string ProceedsAddress,
    decimal PostPurchaseMargin,
    int TotalReleaseQuantity,
    int MaxAllowedPurchaseQuantity,
    DateTime Start,
    DateTime? End = null);

public record SaleContext
(
    Guid SaleWorkerId,
    string SalePath,
    string SaleUtxosPath,
    Sale Sale,
    NiftyCollection Collection,
    List<Nifty> MintableTokens,
    List<Nifty> AllocatedTokens,
    HashSet<Utxo> LockedUtxos,
    HashSet<Utxo> SuccessfulUtxos,
    HashSet<Utxo> RefundedUtxos,
    HashSet<Utxo> FailedUtxos
);

public record PurchaseAttempt(
    Guid Id,
    Guid SaleId,
    Utxo Utxo,
    int NiftyQuantityRequested,
    long ChangeInLovelace);

public enum NiftyDistributionOutcome { 
    Successful = 1, 
    SuccessfulAfterRetry, 
    FailureTxInfo, 
    FailureTxBuild, 
    FailureTxSubmit, 
    FailureUnknown };

public record NiftyDistributionResult(
    NiftyDistributionOutcome Outcome,
    PurchaseAttempt PurchaseAttempt,
    string? MintTxBodyJson,
    string? MintTxHash = null,
    string? BuyerAddress = null,
    Nifty[]? NiftiesDistributed = null,
    Exception? Exception = null);

public record Mint(
    Guid PurchaseAttemptId,
    Guid SaleId,
    string SaleAddress,
    string BuyerAddress,
    string PurchaseAttemptUtxo,
    DateTime MintTxSubmissionAt,
    long PriceLovelaces,
    long CreatorCutLovelaces,
    long MintsafeCutLovelaces,
    bool IsMintsafeCutDistributed,
    Guid NiftyId,
    string PolicyId,
    string AssetName
);
