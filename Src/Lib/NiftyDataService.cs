using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class NiftyDataService
    {
        public Task<CollectionAggregate> GetCollectionAggregateAsync(
            Guid collectionId, CancellationToken ct = default) 
        {
            // Retrieve {Collection * ActiveSales * MintableTokens} from db
            var fakeCollectionId = Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae");
            var collection = new NiftyCollection(
                Id: fakeCollectionId,
                PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                Name: "TOP_SECRET_PROJECT",
                Description: "Creations from TOP_SECRET_PROJECT",
                IsActive: true,
                Publishers: new[] { "TOP_SECRET_PROJECT.io", "NiftyLaunchpad.net" },
                BrandImage: "ipfs://cid",
                CreatedAt: new DateTime(2022, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                LockedAt: new DateTime(2022, 11, 30, 0, 0, 0, DateTimeKind.Utc),
                SlotExpiry: 44674366);

            var tokens = new[]
            {
                new Nifty(
                    Id: Guid.Parse("6f0c17b1-0f65-40af-ba23-7db90a913222"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0001",
                    Name: "TOP_SECRET_PROJECT #1 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Royalty: new Royalty(0, string.Empty),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #1 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("6cf9c598-5925-487d-8497-2d0083c6de10"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0002",
                    Name: "TOP_SECRET_PROJECT #2 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #2 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("934210ea-e118-4c74-9504-400f66655f8c"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0003",
                    Name: "TOP_SECRET_PROJECT #3 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #3 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("cfdc67f0-6383-4eda-a615-e4fc40e5ce4c"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0004",
                    Name: "TOP_SECRET_PROJECT #4 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #4 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("a9e7154d-a587-44d8-9bf0-8c37d5c82740"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0005",
                    Name: "TOP_SECRET_PROJECT #5 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #5 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("bce3060e-e72f-4d70-ac07-fce6bfb564fd"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0006",
                    Name: "TOP_SECRET_PROJECT #6 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #6 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("77ca34e1-657f-47c7-8706-758781f5c61c"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0007",
                    Name: "TOP_SECRET_PROJECT #7 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #7 TOP" }
                    }
                ),
                new Nifty(
                    Id: Guid.Parse("8ca5b728-db6d-4040-a699-5c88e2084a20"),
                    CollectionId: fakeCollectionId,
                    IsMintable: true,
                    AssetName: "topsecret0008",
                    Name: "TOP_SECRET_PROJECT #8 TOP",
                    Description: "TOP M8",
                    Artists: new[] { "TOP_SECRET_PROJECT" },
                    Image: "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i",
                    MediaType: "image/png",
                    Files: new[] {
                        new NiftyFile(Guid.NewGuid(), "Full Resolution", "ipfs://QmV896wmZc6Rp4pqCex5NN2nYUEjh2zFfGkNfC1qe5Dz4i")
                    },
                    Royalty: new Royalty(0, string.Empty),
                    CreatedAt: new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Version: "1.0",
                    Attributes: new Dictionary<string,string>
                    {
                        { "key1", "TOP_SECRET_PROJECT #8 TOP" }
                    }
                ),
            };

            var sale = new NiftySale(
                Id: Guid.Parse("69da836f-9e0b-4ec4-98e8-094efaeac38b"),
                CollectionId: fakeCollectionId,
                IsActive: true,
                Name: "Preview Launch #1",
                Description: "Limited 8 item launch",
                LovelacesPerToken: 10000000,
                SaleAddress: "addr_test1vqv2cldstkyvpjc88w2wd8yuk2g6pltm6lz2jzn0gqke35q5zgwuu",
                DepositAddress: "addr_test1vpfkk396juz65kv3lrq6qjnneyqw4hlu949y8jy8ug465pqrxc3mg",
                TotalReleaseQuantity: 500,
                MaxAllowedPurchaseQuantity: 3);
            
            var activeSales = collection.IsActive && IsSaleOpen(sale) ? new[] { sale } : Array.Empty<NiftySale>();

            return Task.FromResult(new CollectionAggregate(collection, tokens, ActiveSales: activeSales));
        }

        private static bool IsSaleOpen(NiftySale sale)
        {
            if (!sale.IsActive
                || (sale.Start.HasValue && sale.Start < DateTime.UtcNow)
                || (sale.End.HasValue && sale.End > DateTime.UtcNow))
                return false;

            return true;
        }
    }
}
