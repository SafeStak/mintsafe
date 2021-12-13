using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mappers
{
    public static class NiftyMapper
    {
        public static Models.Nifty Map(Nifty nifty)
        {
            return new Models.Nifty
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
                CreatedAt = nifty.CreatedAt.ToUniversalTime(),
                Version = nifty.Version,
                RoyaltyAddress = nifty.Royalty.Address,
                RoyaltyPortion = nifty.Royalty.PortionOfSale,
                Attributes = nifty.Attributes
            };
        }

        public static Nifty Map(Models.Nifty dtoNifty, IEnumerable<Models.NiftyFile> niftyFiles)
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
                niftyFiles.Select(NiftyFileMapper.Map).ToArray(),
                dtoNifty.CreatedAt,
                new Royalty(dtoNifty.RoyaltyPortion, dtoNifty.RoyaltyAddress),
                dtoNifty.Version,
                dtoNifty.Attributes.ToArray()
                );
        }
    }
}
