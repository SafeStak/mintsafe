using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mappers
{
    public static class NiftyFileMapper
    {
        public static NiftyFile Map(Models.NiftyFile niftyFileDto)
        {
            if (niftyFileDto == null) throw new ArgumentNullException(nameof(niftyFileDto));
            if (niftyFileDto.NiftyId == null) throw new ArgumentNullException(nameof(niftyFileDto.NiftyId));
            if (niftyFileDto.Name == null) throw new ArgumentNullException(nameof(niftyFileDto.Name));
            if (niftyFileDto.MediaType == null) throw new ArgumentNullException(nameof(niftyFileDto.MediaType));
            if (niftyFileDto.Src == null) throw new ArgumentNullException(nameof(niftyFileDto.Src));

            return new NiftyFile(
                Guid.Parse(niftyFileDto.RowKey),
                Guid.Parse(niftyFileDto.NiftyId),
                niftyFileDto.Name,
                niftyFileDto.MediaType,
                niftyFileDto.Src,
                niftyFileDto.FileHash ?? string.Empty
            );
        }

        public static Models.NiftyFile Map(Guid collectionId, NiftyFile niftyFile)
        {
            return new Models.NiftyFile
            {
                RowKey = niftyFile.Id.ToString(),
                PartitionKey = collectionId.ToString(),
                NiftyId = niftyFile.NiftyId.ToString(),
                Name = niftyFile.Name,
                MediaType = niftyFile.MediaType,
                Src = niftyFile.Src,
                FileHash = niftyFile.FileHash
            };
        }
    }
}
