using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class CollectionRetriever
    {
        public Task<CollectionAggregate> GetCollectionAsync(Guid guid, CancellationToken ct = default)
        {
            // Retrieve Active Collection + Mintable Tokens from Azure Table Storage
            var collection = new NiftyCollection(
                Id: Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae"),
                PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                Name: "aw0k3n",
                Description: "L1v1ng 0n-ch41n g3n3r4t1v3 4rt.",
                IsActive: true,
                ValidFromSlot: TimeUtil.GetSlotAt(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                ValidToSlot: TimeUtil.GetSlotAt(new DateTime(2022, 10, 26, 0, 0, 0, DateTimeKind.Utc)),
                Publishers: new[] { "niftylaunchpad" },
                PreviewImage: "ipfs://cidpreview",
                CreatedBy: "creator@awoken.io",
                CreatedAt: new DateTime(2022, 6, 4, 0, 0, 0, DateTimeKind.Utc));

            var tokens = new[]
            {
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    AssetName: "awoken7",
                    AssetId: "asset15trltfpz3haxclkr90hwsuptetmlxqh8m6tctx",
                    Name: "aw0k3n algorithm #7",
                    Description: "L1v1ng 0n-ch41n g3n3r4t1v3 4rt.",
                    MediaType: "image/png",
                    Publishers: new[] { "niftylaunchpad" },
                    Image: "ipfs://cidpreview",
                    Files: "ipfs://cidfull",
                    IsMintable: true,
                    CreatedBy: "creator@awoken.io",
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "value" }
                    }
                ),
            };

            return Task.FromResult(new CollectionAggregate(collection, tokens));
        }
    }
}
