using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mapping
{
    public interface INiftyCollectionMapper
    {
        NiftyCollection FromDto(DTOs.NiftyCollection dtoNiftyCollection);
        DTOs.NiftyCollection ToDto(NiftyCollection niftyCollection);
    }
    public class NiftyCollectionMapper : INiftyCollectionMapper
    {
        public DTOs.NiftyCollection ToDto(NiftyCollection niftyCollection)
        {
            return new DTOs.NiftyCollection()
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

        public NiftyCollection FromDto(DTOs.NiftyCollection dtoNiftyCollection)
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
