using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mappers
{ public static class NiftyCollectionMapper
    {
        public static Models.NiftyCollection Map(NiftyCollection niftyCollection)
        {
            return new Models.NiftyCollection
            {
                RowKey = niftyCollection.Id.ToString(),
                PartitionKey = niftyCollection.PolicyId, //TODO PolicyId? Creator?,
                PolicyId = niftyCollection.PolicyId,
                Name = niftyCollection.Name,
                Description = niftyCollection.Description,
                IsActive = niftyCollection.IsActive,
                BrandImage = niftyCollection.BrandImage,
                Publishers = niftyCollection.Publishers,
                CreatedAt = niftyCollection.CreatedAt.ToUniversalTime(),
                LockedAt = niftyCollection.LockedAt.ToUniversalTime(),
                SlotExpiry = niftyCollection.SlotExpiry
            };
        }

        public static NiftyCollection Map(Models.NiftyCollection dtoNiftyCollection)
        {
            return new NiftyCollection(
                Guid.Parse(dtoNiftyCollection.RowKey),
                dtoNiftyCollection.PolicyId,
                dtoNiftyCollection.Name,
                dtoNiftyCollection.Description,
                dtoNiftyCollection.IsActive,
                dtoNiftyCollection.BrandImage,
                dtoNiftyCollection.Publishers,
                dtoNiftyCollection.CreatedAt,
                dtoNiftyCollection.LockedAt,
                dtoNiftyCollection.SlotExpiry);
        }
    }
}
