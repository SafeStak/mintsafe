using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mapping
{
    public interface INiftyCollectionMapper
    {
        NiftyCollection Map(Models.NiftyCollection dtoNiftyCollection);
        Models.NiftyCollection Map(NiftyCollection niftyCollection);
    }
    public class NiftyCollectionMapper : INiftyCollectionMapper
    {
        public Models.NiftyCollection Map(NiftyCollection niftyCollection)
        {
            return new Models.NiftyCollection()
            {
                RowKey = niftyCollection.Id.ToString(),
                PartitionKey = niftyCollection.PolicyId, //TODO PolicyId? Creator?,
                PolicyId = niftyCollection.PolicyId,
                Name = niftyCollection.Name,
                Description = niftyCollection.Description,
                IsActive = niftyCollection.IsActive,
                BrandImage = niftyCollection.BrandImage,
                Publishers = niftyCollection.Publishers,
                CreatedAt = niftyCollection.CreatedAt,
                LockedAt = niftyCollection.LockedAt,
                SlotExpiry = niftyCollection.SlotExpiry
            };
        }

        public NiftyCollection Map(Models.NiftyCollection dtoNiftyCollection)
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
