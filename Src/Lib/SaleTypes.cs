using System;

namespace NiftyLaunchpad.Lib
{
    public record SaleEvent(
        Guid Id,
        string CollectionId,
        string PolicyId,
        string LovelacesPerToken,
        string SaleAddress,
        bool IsActive,
        int ValidFromSlot,
        int ValidToSlot,
        int ReleaseQuantity,
        int MaxAllowedPurchaseQuantity
    );
}
