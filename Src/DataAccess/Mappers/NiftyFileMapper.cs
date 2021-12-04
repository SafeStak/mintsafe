using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mappers
{
    public static class NiftyFileMapper
    {
        public static NiftyFile Map(Models.NiftyFile niftyFileDto)
        {
            return new NiftyFile(
                Guid.Parse(niftyFileDto.RowKey),
                Guid.Parse(niftyFileDto.NiftyId),
                niftyFileDto.Name,
                niftyFileDto.MediaType,
                niftyFileDto.Url,
                niftyFileDto.FileHash
            );
        }

        public static Models.NiftyFile Map(NiftyFile niftyFile)
        {
            return new Models.NiftyFile
            {
                RowKey = niftyFile.Id.ToString(),
                //PartitionKey = collectionId.ToString(),
                NiftyId = niftyFile.NiftyId.ToString(),
                Name = niftyFile.Name,
                MediaType = niftyFile.MediaType,
                Url = niftyFile.Url,
                FileHash = niftyFile.FileHash
            };
        }
    }
}
