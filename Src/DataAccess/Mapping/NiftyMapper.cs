using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mapping
{
    public interface INiftyMapper
    {
        Nifty FromDto(DTOs.Nifty dtoNifty);
        DTOs.Nifty ToDto(Nifty nifty);
    }

    public class NiftyMapper : INiftyMapper
    {
        public DTOs.Nifty ToDto(Nifty nifty)
        {
            return new DTOs.Nifty()
            {
                RowKey = nifty.Id.ToString(),
                PartitionKey = nifty.CollectionId.ToString(),
                IsMintable = nifty.IsMintable,
                AssetName = nifty.AssetName,
                Name = nifty.Name,
                Description = nifty.Description,
                Creators = nifty.Creators,
                Image = nifty.Image,
                MediaType = nifty.MediaType,
                CreatedAt = nifty.CreatedAt,
                Version = nifty.Version,
                RoyaltyAddress = nifty.Royalty.Address,
                RoyaltyPortion = nifty.Royalty.PortionOfSale
            };
        }

        public Nifty FromDto(DTOs.Nifty dtoNifty)
        {
            return new Nifty(
                Guid.Parse(dtoNifty.RowKey),
                Guid.Parse(dtoNifty.PartitionKey),
                dtoNifty.IsMintable,
                dtoNifty.AssetName,
                dtoNifty.Name,
                dtoNifty.Description,
                dtoNifty.Creators,
                dtoNifty.Image,
                dtoNifty.MediaType,
                null, //TODO map later on not in repo
                dtoNifty.CreatedAt,
                new Royalty(dtoNifty.RoyaltyPortion, dtoNifty.RoyaltyAddress),
                dtoNifty.Version,
                null //TODO dictionary flatten
                );
        }
    }
}
