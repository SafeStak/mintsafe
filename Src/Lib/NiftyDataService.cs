using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class NiftyDataService
    {
        public Task<CollectionAggregate> GetCollectionAsync(
            Guid collectionId, CancellationToken ct = default)
        {
            // Retrieve Active Collection + Mintable Tokens from Azure Table Storage
            var collection = new NiftyCollection(
                Id: Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae"),
                PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                Name: "BRIKMAESTRO",
                Description: "Creations from the BRIKAVERSE",
                IsActive: true,
                Publishers: new[] { "BRIKMAESTRO", "NiftyLaunchpad.net" },
                BrandImage: "ipfs://cid",
                CreatedAt: new DateTime(2022, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                LockedAt: new DateTime(2022, 11, 30, 0, 0, 0, DateTimeKind.Utc));

            var tokens = new[]
            {
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0001",
                    Name: "BRIKMAESTRO #1 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #1 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0002",
                    Name: "BRIKMAESTRO #2 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #2 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0003",
                    Name: "BRIKMAESTRO #3 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #3 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0004",
                    Name: "BRIKMAESTRO #4 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #4 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0005",
                    Name: "BRIKMAESTRO #5 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #5 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0006",
                    Name: "BRIKMAESTRO #6 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #6 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0007",
                    Name: "BRIKMAESTRO #7 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #7 BRIKKED" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                    IsMintable: true,
                    AssetName: "brikmaestro0008",
                    Name: "BRIKMAESTRO #8 BRIKKED",
                    Description: "BRIKKED M8",
                    Artists: new[] { "BRIKMAESTRO" },
                    Image: "ipfs://cidpreview",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://cidfull")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "BRIKMAESTRO #8 BRIKKED" }
                    }
                ),
            };

            var sale = new NiftySalePeriod(
                Id: Guid.Parse("69da836f-9e0b-4ec4-98e8-094efaeac38b"),
                CollectionId: collectionId,
                PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                IsActive: true,
                Name: "Preview Launch #1",
                Description: "Limited 500 item launch",
                LovelacesPerToken: 10000000,
                SaleAddress: "addr_test1vrldgv89yh0edkuwrvkkhc3yx4npfccdvtz7dfkn85a78rsu9nkm4",
                TotalReleaseQuantity: 500,
                MaxAllowedPurchaseQuantity: 5);
            
            var activeSales = collection.IsActive && IsSaleOpen(sale) ? new[] { sale } : Array.Empty<NiftySalePeriod>();

            return Task.FromResult(new CollectionAggregate(collection, tokens, ActiveSales: activeSales));
        }

        private static bool IsSaleOpen(NiftySalePeriod sale)
        {
            if (!sale.IsActive
                || (sale.Start.HasValue && sale.Start < DateTime.UtcNow)
                || (sale.End.HasValue && sale.End > DateTime.UtcNow))
                return false;

            return true;
        }
    }
}
