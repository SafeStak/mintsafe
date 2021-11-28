using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mapping
{
    public interface INiftyMapper
    {
        Nifty Map(Models.Nifty dtoNifty, IEnumerable<Models.NiftyFile> niftyFiles);
        Models.Nifty Map(Nifty nifty);
    }

    public class NiftyMapper : INiftyMapper
    {
        private readonly INiftyFileMapper _niftyFileMapper;

        public NiftyMapper(INiftyFileMapper niftyFileMapper)
        {
            _niftyFileMapper = niftyFileMapper;
        }

        public Models.Nifty Map(Nifty nifty)
        {
            return new Models.Nifty()
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
                RoyaltyPortion = nifty.Royalty.PortionOfSale,
                Attributes = nifty.Attributes
            };
        }

        public Nifty Map(Models.Nifty dtoNifty, IEnumerable<Models.NiftyFile> niftyFiles)
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
                niftyFiles.Select(_niftyFileMapper.Map).ToArray(),
                dtoNifty.CreatedAt,
                new Royalty(dtoNifty.RoyaltyPortion, dtoNifty.RoyaltyAddress),
                dtoNifty.Version,
                dtoNifty.Attributes
                );
        }
    }
}
