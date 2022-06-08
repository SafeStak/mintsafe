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
                Attributes = nifty.Attributes
            };
        }

        public static Nifty Map(Models.Nifty dtoNifty, IEnumerable<Models.NiftyFile> niftyFiles)
        {
            if (dtoNifty == null) throw new ArgumentNullException(nameof(dtoNifty));
            if (dtoNifty.AssetName == null) throw new ArgumentNullException(nameof(dtoNifty.AssetName));
            if (dtoNifty.Name == null) throw new ArgumentNullException(nameof(dtoNifty.Name));
            if (dtoNifty.RowKey == null) throw new ArgumentNullException(nameof(dtoNifty.RowKey));
            if (dtoNifty.PartitionKey == null) throw new ArgumentNullException(nameof(dtoNifty.PartitionKey));

            return new Nifty(
                Guid.Parse(dtoNifty.RowKey),
                Guid.Parse(dtoNifty.PartitionKey),
                dtoNifty.IsMintable,
                dtoNifty.AssetName,
                dtoNifty.Name,
                dtoNifty.Description,
                dtoNifty.Creators ?? Array.Empty<string>(),
                dtoNifty.Image,
                dtoNifty.MediaType,
                niftyFiles.Select(NiftyFileMapper.Map).ToArray(),
                dtoNifty.CreatedAt,
                dtoNifty.Version,
                dtoNifty.Attributes == null 
                    ? Array.Empty<KeyValuePair<string,string>>() : dtoNifty.Attributes.ToArray()
                );
        }
    }
}
