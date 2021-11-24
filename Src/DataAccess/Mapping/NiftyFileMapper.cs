using Mintsafe.Abstractions;

namespace Mintsafe.DataAccess.Mapping
{
    public interface INiftyFileMapper
    {
        NiftyFile Map(Models.NiftyFile dtoNifty);
        Models.NiftyFile Map(NiftyFile nifty);
    }

    public class NiftyFileMapper : INiftyFileMapper
    {
        public NiftyFile Map(Models.NiftyFile niftyFileDto)
        {
            return new NiftyFile(
                Guid.Parse(niftyFileDto.RowKey),
                Guid.Parse(niftyFileDto.PartitionKey),
                niftyFileDto.Name,
                niftyFileDto.MediaType,
                niftyFileDto.Url,
                niftyFileDto.FileHash
            );
        }

        public Models.NiftyFile Map(NiftyFile niftyFile)
        {
            return new Models.NiftyFile()
            {
                RowKey = niftyFile.Id.ToString(),
                PartitionKey = niftyFile.NiftyId.ToString(),
                Name = niftyFile.Name,
                MediaType = niftyFile.MediaType,
                Url = niftyFile.Url,
                FileHash = niftyFile.FileHash
            };
        }
    }
}
