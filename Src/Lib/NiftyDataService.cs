using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class NiftyDataService
    {
        public const string FakeCollectionId = "d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae";
        public const string FakeSaleId = "69da836f-9e0b-4ec4-98e8-094efaeac38b";

        public Task<CollectionAggregate> GetCollectionAggregateAsync(
            Guid collectionId, CancellationToken ct = default) 
        {
            // Retrieve {Collection * ActiveSales * MintableTokens} from db
            var fakeCollectionId = Guid.Parse(FakeCollectionId);
            var collection = new NiftyCollection(
                Id: fakeCollectionId,
                PolicyId: "e9b6f907ea790ca51957eb513430eb0ec155f8df654d48e961d7ea3e",
                Name: "cryptoquokkas",
                Description: "Creations from TOP_SECRET_PROJECT",
                IsActive: true,
                Publishers: new[] { "cryptoquokkas.io", "mintsafe.io" },
                BrandImage: "ipfs://cid",
                CreatedAt: new DateTime(2021, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                LockedAt: new DateTime(2021, 12, 25, 0, 0, 0, DateTimeKind.Utc),
                SlotExpiry: 46021186); // testnet christmas 

            var tokens = GenerateTokens(
                1000,
                FakeCollectionId);

            var sale = new NiftySale(
                Id: Guid.Parse(FakeSaleId),
                CollectionId: fakeCollectionId,
                IsActive: true,
                Name: "Preview Launch #1",
                Description: "Limited 150 item launch",
                LovelacesPerToken: 3000000,
                SaleAddress: "addr_test1vz0hx28mmdz0ey3pzqe5nxg08urjhzydpvvmcx4v4we5mvg6733n5",
                ProceedsAddress: "addr_test1vzj4c522pr5n6texvcl24kl9enntr4knl4ucecd7pkt24mglna4pz",
                TotalReleaseQuantity: 150,
                MaxAllowedPurchaseQuantity: 150);
            
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

        public static Nifty[] GenerateTokens(
            int mintableTokenCount, 
            string collectionId = null, 
            bool isMintable = true,
            string baseName = "cryptoquokkas",
            string creatorsCsv = "cryptoquokkas.io,mintsafe.io",
            string urlBase = "https://cryptoquokkas.io/ms/",
            string mediaType = "image/png",
            string createdAtIso8601 = "2021-01-01T19:30:00Z",
            double royaltyPortion = 0,
            string royaltyAddress = "",
            string version = "1",
            string attributeKey = "size",
            string attributeValue = "full")
        {
            DateTime.TryParseExact(
                createdAtIso8601,
                @"yyyy-MM-dd\THH:mm:ss\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dateTimeParsed);

            Dictionary<string, string> GetAttributesForIndex(int i)
            {
                return new Dictionary<string, string>
                {
                    { attributeKey, $"{attributeValue}{i}" },
                    { "seq", $"{i}" },
                    { "hash", $"b87f88c72702fff1748e58b87e9141a42c0dbedc29a78cb0d4a5cd81" },
                };
            }

            return Enumerable.Range(0, mintableTokenCount)
                .Select(i => new Nifty(
                    Guid.NewGuid(),
                    collectionId == null ? Guid.NewGuid() : Guid.Parse(collectionId),
                    isMintable,
                    $"{baseName}{i}",
                    $"{baseName} {i}",
                    $"{baseName} {i} Description",
                    creatorsCsv.Split(','),
                    $"{urlBase}{i}.png",
                    mediaType,
                    new[] {
                        new NiftyFile(Guid.NewGuid(), "full_res_png", "image/png", $"{urlBase}{i}.png"),
                        new NiftyFile(Guid.NewGuid(), "specs_pdf", "application/pdf", $"{urlBase}{i}.pdf")
                    },
                    dateTimeParsed.AddDays(i),
                    new Royalty(royaltyPortion, royaltyAddress),
                    version,
                    GetAttributesForIndex(i)))
                .ToArray();
        }
    }
}
