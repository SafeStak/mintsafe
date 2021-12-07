using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class LocalNiftyDataService : INiftyDataService
{
    public const string FakeCollectionId = "d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae";
    public const string FakeSaleId = "d91b937f-00fc-4094-957c-629fe3e2e776";

    public Task<CollectionAggregate> GetCollectionAggregateAsync(
        Guid collectionId, CancellationToken ct = default)
    {
        // Retrieve {Collection * ActiveSales * MintableTokens} from db
        var fakeCollectionId = Guid.Parse(FakeCollectionId);
        var collection = new NiftyCollection(
            Id: fakeCollectionId,
            PolicyId: "fbd42bedfcf8d5de2381dd572676dd5e85fd09b2a45ba80358d20fea",
            Name: "cryptoquokkas",
            Description: "Creations from TOP_SECRET_PROJECT",
            IsActive: true,
            Publishers: new[] { "cryptoquokkas.io", "mintsafe.io" },
            BrandImage: "ipfs://cid",
            CreatedAt: new DateTime(2021, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            LockedAt: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SlotExpiry: 46021186); // testnet christmas 

        var tokens = GenerateTokens(
            3000,
            FakeCollectionId);

        var sale = new Sale(
            Id: Guid.Parse(FakeSaleId),
            CollectionId: fakeCollectionId,
            IsActive: true,
            Name: "Preview Launch #1",
            Description: "Limited 150 item launch",
            LovelacesPerToken: 36000000,
            Start: new DateTime(2021, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            End: new DateTime(2021, 12, 8, 0, 0, 0, DateTimeKind.Utc),
            SaleAddress: "addr_test1vqgh0dutf08aynjcvhwa8jeaclpxs29fpjtsunlw2056pycjut5w7",
            ProceedsAddress: "addr_test1vp92pf7y6mk9qgqs2474mxvjh9u3e5h885v6hy8c8qp3wdcddsldj",
            TotalReleaseQuantity: 3000,
            MaxAllowedPurchaseQuantity: 4);

        var activeSales = collection.IsActive && IsSaleOpen(sale) ? new[] { sale } : Array.Empty<Sale>();

        return Task.FromResult(new CollectionAggregate(collection, tokens, ActiveSales: activeSales));
    }

    public Task InsertCollectionAggregateAsync(CollectionAggregate collectionAggregate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private static bool IsSaleOpen(Sale sale)
    {
        if (!sale.IsActive
            || (sale.Start.HasValue && sale.Start > DateTime.UtcNow)
            || (sale.End.HasValue && sale.End < DateTime.UtcNow))
            return false;

        return true;
    }

    private static Nifty[] GenerateTokens(
        int mintableTokenCount,
        string? collectionId = null,
        bool isMintable = true,
        string baseName = "cryptoquokkas",
        string creatorsCsv = "quokkalad.ada",
        string urlBase = "ipfs://QmXoypizjW3WknFiJnKLwHCnL72vedxjQkDDP1mXWo6",
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
            .Select(i =>
            {
                var niftyId = Guid.NewGuid();
                return new Nifty(
                    niftyId,
                    collectionId == null ? Guid.NewGuid() : Guid.Parse(collectionId),
                    isMintable,
                    $"{baseName}{i}",
                    $"{baseName} {i}",
                    $"{baseName} {i} Description",
                    creatorsCsv.Split(','),
                    $"{urlBase}{i + 2}",
                    mediaType,
                    new NiftyFile[]
                    {
                        new(Guid.NewGuid(), niftyId, "full_res_png", "image/png", $"{urlBase}{i}"),
                        new(Guid.NewGuid(), niftyId, "specs_pdf", "application/pdf", $"{urlBase}{i + 1}")
                    },
                    dateTimeParsed.AddDays(i),
                    new Royalty(royaltyPortion, royaltyAddress),
                    version,
                    GetAttributesForIndex(i));
            })
            .ToArray();
    }
}
