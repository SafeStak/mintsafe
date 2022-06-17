using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class LocalNiftyDataService : INiftyDataService
{
    public const string FakeCollectionId = "4f03062a-460b-4946-a66a-be481cd8788f";
    public const string FakeSaleId = "7ca72580-4285-43f4-a7bb-5a465a9bdf85";

    public Task<CollectionAggregate?> GetCollectionAggregateAsync(
        Guid collectionId, CancellationToken ct = default)
    {
        // Retrieve {Collection * ActiveSales * MintableTokens} from db
        var fakeCollectionId = Guid.Parse(FakeCollectionId);
        var collection = new NiftyCollection(
            Id: fakeCollectionId,
            PolicyId: "fbd42bedfcf8d5de2381dd572676dd5e85fd09b2a45ba80358d20fea",
            Name: "cryptoquokkas",
            Description: "cryptoquokkas.com",
            IsActive: true,
            Publishers: new[] { "cryptoquokkas.com", "mintsafe.io" },
            BrandImage: "",
            CreatedAt: new DateTime(2022, 1, 28, 0, 0, 0, DateTimeKind.Utc),
            LockedAt: new DateTime(2022, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            SlotExpiry: 74686216,
            Royalty: new Royalty(0, string.Empty)); // testnet christmas 

        var tokens = GenerateTokens(
            100,
            FakeCollectionId);

        var sale = new Sale(
            Id: Guid.Parse(FakeSaleId),
            CollectionId: fakeCollectionId,
            IsActive: true,
            Name: "Launch #1",
            Description: "Limited 40 item launch",
            LovelacesPerToken: 45000000,
            Start: new DateTime(2022, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            End: new DateTime(2022, 12, 14, 0, 0, 0, DateTimeKind.Utc),
            SaleAddress: "addr_test1vqgh0dutf08aynjcvhwa8jeaclpxs29fpjtsunlw2056pycjut5w7",
            CreatorAddress: "addr_test1vp92pf7y6mk9qgqs2474mxvjh9u3e5h885v6hy8c8qp3wdcddsldj",
            ProceedsAddress: "addr_test1vp92pf7y6mk9qgqs2474mxvjh9u3e5h885v6hy8c8qp3wdcddsldj",
            PostPurchaseMargin: 0.1m,
            TotalReleaseQuantity: 45,
            MaxAllowedPurchaseQuantity: 1);

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
            || (sale.Start > DateTime.UtcNow)
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
                    version,
                    GetAttributesForIndex(i).ToArray());
            })
            .ToArray();
    }
}

public class TacfDataService : INiftyDataService
{
    public const string TicketCollectionId = "9e0d7f84-9b47-404a-b32e-c86f62d512dc";
    public const string TicketSaleId = "cb0885b1-48c4-433a-ac56-cd12fccef663";

    public Task<CollectionAggregate?> GetCollectionAggregateAsync(Guid collectionId, CancellationToken ct = default)
    {
        var fakeCollectionId = Guid.Parse(TicketCollectionId);

        var sale = new Sale(
            Id: Guid.Parse(TicketSaleId),
            CollectionId: fakeCollectionId,
            IsActive: true,
            Name: "Ticket Sale #1",
            Description: "",
            LovelacesPerToken: 125_000000,
            Start: new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            End: new DateTime(2023, 7, 23, 0, 0, 0, DateTimeKind.Utc),
            SaleAddress: "addr_test1vq3qme7zjxj8xsn547s7mf0j9t9x8h2x30f00vsvhag3g6snqkn9v", // addr1vy3qme7zjxj8xsn547s7mf0j9t9x8h2x30f00vsvhag3g6sggz02f
            CreatorAddress: "addr_test1qpegeshnlug9g88xla67d9f6xz47z4fsx9jfyy35z04ekrzjdx9mm3ju60d74u9p86uukq9ldvjza3ycr5d9jlvqxeqsdwyruy", // ?
            ProceedsAddress: "addr_test1vz93vkgv8kg5lralfmspl92c039hlu7y3vpjrccpcjg3l7qzflgnf", // addr1vx93vkgv8kg5lralfmspl92c039hlu7y3vpjrccpcjg3l7qept5uv
            PostPurchaseMargin: 0.1m,
            TotalReleaseQuantity: 100,
            MaxAllowedPurchaseQuantity: 1);

        var tokens = Enumerable.Range(1, 100)
            .Select(i =>
                new Nifty(
                    Guid.NewGuid(),
                    fakeCollectionId,
                    true,
                    $"TACF_GCST_C_{i}",
                    $"TACF Global Concert Series Ticket C{i}",
                    null,
                    new[] { "takialsop.org" },
                    "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.1' viewBox='0 0 319.3 236'><path d='M0 155h319.3v81H0z'/><g fill='%23fff'><path d='M22 213.7c-1.3-1.6-3.2-2.6-5.2-2.6-4 .2-7.1 3.7-6.9 7.7.2 3.7 3.2 6.7 6.9 6.9 2.9 0 5.5-1.9 6.5-4.7h-8.8v-2.5h12c.1 5.5-4.3 10.1-9.9 10.1-5.5.1-10.1-4.3-10.1-9.9s4.3-10.1 9.9-10.1c3.6 0 7 1.9 8.8 5.1H22zm7.4 14.2v-19h2.8v16.4h6.6v2.5h-9.4v.1z'/><use xlink:href='%23B'/><path d='M62.3 209h4.9c5.4 0 5.9 3.9 5.9 5.3a4.6 4.6 0 0 1-1.7 3.6c1.7.9 2.8 2.7 2.8 4.6s-1.1 5.5-6.3 5.5h-5.7l.1-19h0zm2.8 7.9h2.3c2.6 0 3-1.6 3-2.7 0-2.7-2.6-2.7-3.2-2.7h-2.1v5.4zm0 8.5h3c2.8 0 3.4-2 3.4-3 0-.7-.3-1.4-.7-1.9-.8-.9-1.9-1.3-3.1-1.2H65l.1 6.1h0zm24.2 2.5l-2.1-5.6h-6.8l-2.1 5.6h-2.9l7.2-19H85l7.2 19h-2.9zm-3-8.1l-2.4-7.3h-.1l-2.4 7.3h4.9zm8.3 8.1v-19h2.8v16.4h6.6v2.5h-9.4v.1z'/></g><g fill='%23d01f3c'><use xlink:href='%23C'/><use xlink:href='%23B' x='92.3'/><path d='M166.8 227.9l-9.4-14.7h-.1v14.7h-2.8v-19h3l9.2 14.4h.1V209h2.8v19h-2.8v-.1z'/><use xlink:href='%23C' x='60.6'/><path d='M193.3 227.9v-19h10.2v2.5H196v5.6h7.5v2.5H196v5.8h7.5v2.5l-10.2.1h0z'/><use xlink:href='%23D'/><path d='M224.4 227.9v-16.4h-3.6V209h10v2.5h-3.6v16.4h-2.8z'/></g><g fill='%23fff'><path d='M241.3 222.3c0 .6.3 3.4 3.1 3.4 1.6 0 3-1.2 3-2.9v-.3c0-2.1-1.7-2.6-3-3.1-2.5-1-3.2-1.3-4-2.1-.9-1-1.3-2.3-1.3-3.6 0-2.8 2.4-5.1 5.2-5.1h.1c2.8-.2 5.2 1.9 5.4 4.7v.6H247c.1-1.4-.9-2.6-2.3-2.7h-.4c-1.4.1-2.5 1.2-2.5 2.6 0 1.8 1.7 2.3 2.5 2.6 2.4.9 5.8 1.6 5.8 6.2.1 3-2.2 5.6-5.3 5.7h-.5c-3.1.1-5.7-2.4-5.8-5.5v-.5h2.8zm11.2 5.6v-19h10.2v2.5h-7.5v5.6h7.5v2.5h-7.5v5.8h7.5v2.5l-10.2.1h0z'/><use xlink:href='%23D' x='59.2'/><path d='M281.3 227.9v-19h2.8v19h-2.8zm6.4 0v-19h10.2v2.5h-7.5v5.6h7.5v2.5h-7.5v5.8h7.5v2.5l-10.2.1h0zm14.8-5.6c0 .6.3 3.4 3.1 3.4 1.6 0 3-1.2 3-2.9v-.3c0-2.1-1.7-2.6-3-3.1-2.5-1-3.2-1.3-4-2.1-.9-1-1.3-2.3-1.3-3.6 0-2.8 2.4-5.1 5.2-5.1h.1c2.8-.2 5.2 1.9 5.4 4.7v.6h-2.8c.1-1.4-.9-2.6-2.3-2.7h-.4c-1.4.1-2.5 1.2-2.5 2.6 0 1.8 1.7 2.3 2.5 2.6 2.4.9 5.8 1.6 5.8 6.2.1 3-2.2 5.6-5.3 5.7h-.5c-3.1.1-5.7-2.4-5.8-5.5v-.5h2.8z'/></g><path d='M79.1 106.1v14.4h13.4v5.9H79.1v20.4h-5.9v-46.7h19.3v5.9l-13.4.1zm23.6 0v14.4h13.4v5.9h-13.4v14.5h13.4v5.9H96.7v-46.7H116v5.9l-13.3.1zm17.7 40.8v-46.7h5.9V141h14.4v5.9h-20.3zm22.5 0v-46.7h5.9V141h14.4v5.9h-20.3zm33.8 0c-1.9.1-3.8-.3-5.4-1.2-1.2-.6-2.2-1.5-3-2.5-1.8-2.3-2.6-5.5-2.6-9.7v-20.1c0-4.1.9-7.4 2.6-9.7.4-.5.8-.9 1.2-1.3.6-.5 1.2-.9 1.8-1.2.8-.4 1.6-.7 2.4-.9 1-.2 2-.3 3-.3 1.9-.1 3.8.3 5.4 1.2 1.1.7 2.2 1.5 3 2.5 1.8 2.3 2.6 5.5 2.6 9.7v20.1c0 4.1-.9 7.4-2.6 9.7-.9 1-1.9 1.8-3 2.5-1.7.8-3.5 1.2-5.4 1.2zm0-40.8c-.5 0-1 0-1.5.1-.6.1-1.2.4-1.7.8-.6.6-1.1 1.3-1.4 2.1-.4 1.4-.6 2.8-.5 4.2v20.1c0 1.5.1 2.9.5 4.3.2.8.7 1.6 1.4 2.1.5.4 1.1.7 1.7.8.5.1 1 .1 1.5.1s1 0 1.5-.1c.6-.1 1.2-.4 1.7-.8.6-.6 1.1-1.3 1.4-2.1.4-1.4.6-2.9.5-4.3v-20.1c0-1.4-.1-2.9-.5-4.2-.3-.8-.7-1.5-1.4-2.1-.5-.4-1.1-.7-1.7-.8-.6 0-1.1-.1-1.5-.1zm56.3-6L222.6 147h-4.8l-6.6-30-6.6 30h-4.8l-10.4-46.7h6.1l6.6 29.7 6.6-29.8h4.8l6.6 29.8 6.6-29.8 6.3-.1h0zm11.6 46.8c-3.7 0-6.5-1.2-8.5-3.6-1.8-2.2-2.7-5.4-2.7-9.7v-3h6v3c0 2.7.5 4.7 1.4 5.9.3.4.7.7 1.1.9.8.4 1.7.6 2.6.5 1 .1 1.9-.2 2.7-.7.7-.4 1.2-1.1 1.5-1.8.4-.7.6-1.5.7-2.3.1-.7.2-1.5.2-2.2 0-1-.2-1.9-.5-2.8-.3-.7-.7-1.4-1.3-1.9a7.49 7.49 0 0 0-1.8-1.3c-.6-.4-1.3-.7-2-1l-1.1-.5c-.7-.3-1.5-.7-2.6-1.2-1.1-.6-2.2-1.4-3.1-2.2-2.5-2.4-3.9-5.8-3.8-9.4 0-1.8.3-3.7.8-5.4.5-1.5 1.3-3 2.3-4.2s2.2-2.1 3.5-2.7c1.4-.7 3-1 4.5-1 1.9-.1 3.8.3 5.4 1.2 1.1.7 2.2 1.5 3 2.5 1.8 2.3 2.7 5.5 2.7 9.7v3h-6v-3c0-1.4-.1-2.9-.5-4.2-.3-.8-.8-1.6-1.4-2.2-.5-.4-1.1-.7-1.7-.8-.5-.1-1-.1-1.5-.1-1-.1-2 .3-2.8.9-.7.6-1.2 1.3-1.5 2.1-.4.8-.6 1.6-.7 2.5-.1.6-.1 1.3-.1 1.9 0 1 .2 1.9.5 2.8.3.8.7 1.4 1.3 2 .5.6 1.2 1.1 1.9 1.4.7.4 1.5.7 2.2 1l1 .4c.6.3 1.4.6 2.4 1.1 1.1.6 2.2 1.3 3.1 2.2 2.6 2.4 4 5.8 3.9 9.4.1 3.2-.8 6.4-2.6 9-2 2.5-5.2 3.9-8.5 3.8zm35-46.8v46.7h-6v-20.3h-9v20.3h-6v-46.7h6v20.3h9v-20.3h6zm6.9 46.9v-46.9h5.9v46.7h-5.9v.2zm16.9-.1h-6v-46.7h9.7c3.9 0 7.1 1.2 9.1 3.5s2.8 5.4 2.8 9.7-.9 7.6-2.8 9.7c-2 2.4-5 3.5-9.1 3.5h-3.7v20.3zm0-26.4h3.7c1.1.1 2.3-.1 3.3-.6.4-.2.8-.5 1.1-.9.9-1 1.4-2.9 1.4-5.7 0-1.3-.1-2.5-.4-3.7-.2-.8-.5-1.5-1.1-2.1-.5-.5-1.1-.8-1.8-1-.9-.2-1.7-.3-2.6-.3h-3.7v14.4h.1v-.1z'/><path d='M83.8 94c-1.1 0-2.1-.1-3.1-.4l-2.4-1c-.7-.4-1.3-.8-1.8-1.3s-.9-1-1.3-1.5c-1.8-2.5-2.7-6.1-2.7-10.9V56.4c0-4.7.9-8.3 2.7-10.9.9-1.1 1.9-2.1 3.1-2.8 1.7-1 3.7-1.4 5.6-1.3 1.9-.1 3.9.3 5.5 1.3 1.2.8 2.3 1.7 3.1 2.8 1.9 2.6 2.8 6.2 2.8 10.9v3.3h-6.2v-3.3c.1-1.6-.1-3.2-.6-4.7-.3-.9-.7-1.7-1.4-2.4-.5-.5-1.1-.8-1.7-.9-1.1-.1-2.2-.1-3.2 0-.7.1-1.3.4-1.7.9-.7.7-1.2 1.5-1.4 2.4-.4 1.5-.6 3.1-.5 4.7V79c0 3.1.5 5.3 1.5 6.8.3.4.7.8 1.2 1.1 1.7.8 3.6.8 5.2 0 .4-.3.9-.7 1.2-1.1 1-1.4 1.4-3.6 1.4-6.8v-3.3h6.2V79c0 4.6-.9 8.3-2.8 10.9-.9 1.1-1.9 2.1-3.1 2.8-1.7 1-3.6 1.4-5.6 1.3zm26.5 0c-1.9.1-3.9-.4-5.5-1.3-1.2-.7-2.3-1.7-3.1-2.8C99.9 87.4 99 83.7 99 79V56.4c0-4.7.9-8.3 2.7-10.9.4-.5.8-1 1.3-1.4a7.49 7.49 0 0 1 1.8-1.3c.8-.4 1.6-.7 2.4-1 2.9-.8 6-.4 8.8.9 1.2.7 2.3 1.7 3.1 2.8 1.8 2.5 2.7 6.1 2.7 10.9V79c0 4.6-.9 8.3-2.7 10.9-.9 1.1-1.9 2.1-3.1 2.8-1.8.9-3.7 1.4-5.7 1.3zm0-45.8c-.5 0-1.1 0-1.6.1-.6.1-1.2.4-1.7.9-.7.7-1.2 1.5-1.4 2.4-.5 1.5-.7 3.1-.6 4.7v22.5c-.1 1.6.1 3.3.6 4.8.3.9.8 1.8 1.4 2.4.5.5 1.1.8 1.7.9.5.1 1.1.1 1.6.1s1.1 0 1.6-.1c.7-.1 1.3-.5 1.8-.9.7-.7 1.2-1.5 1.4-2.4.5-1.6.7-3.2.6-4.8V56.4c.1-1.6-.1-3.2-.6-4.7-.3-.9-.7-1.7-1.4-2.4a3.1 3.1 0 0 0-1.8-.9c-.5-.2-1-.2-1.6-.2h0zM132.6 94h-6.2V41.4h5.1l11.8 31.8V41.4h6.2V94h-5.2l-11.8-31.8.1 31.8h0zm31.3 0h-9.7V41.5h9.7c4.1 0 7.2 1.4 9.4 4.2 1.9 2.5 2.9 6.1 2.9 11v22c0 4.8-1 8.5-2.9 11.1s-5.2 4.2-9.4 4.2zm-3.4-6.7h3.5c.7 0 1.5-.1 2.2-.2.7-.2 1.4-.6 1.9-1.2.6-.7 1.1-1.6 1.3-2.5.4-1.5.5-3.1.5-4.6V56.6c0-1.6-.1-3.1-.5-4.6-.2-.9-.7-1.8-1.3-2.5-.5-.6-1.2-.9-1.9-1.1-.7-.1-1.5-.2-2.2-.2h-3.5v39.1zm30.3 6.7c-1.8.1-3.6-.3-5.2-1.2-1.2-.7-2.2-1.6-2.9-2.7-1.7-2.3-2.6-5.7-2.6-10.1V41.4h6.1V80c0 1.4.1 2.9.5 4.2.2.8.6 1.6 1.2 2.2a3.15 3.15 0 0 0 1.5.8c.5.1.9.1 1.4.1s.9 0 1.4-.1c.6-.1 1.1-.4 1.5-.8.6-.6 1-1.4 1.2-2.2.4-1.4.5-2.8.5-4.2V41.4h6.1V80c0 4.3-.9 7.8-2.6 10.1-.8 1-1.8 2-2.9 2.7-1.6.9-3.4 1.3-5.2 1.2zm26.3 0c-1.1 0-2.1-.1-3.1-.4l-2.4-1c-.7-.4-1.3-.8-1.8-1.3s-.9-1-1.3-1.5c-1.8-2.5-2.7-6.1-2.7-10.9V56.4c0-4.7.9-8.3 2.7-10.9.9-1.1 1.9-2.1 3.1-2.8 1.7-1 3.7-1.4 5.6-1.3 1.9-.1 3.9.3 5.5 1.3 1.2.8 2.3 1.7 3.1 2.8 1.9 2.6 2.8 6.2 2.8 10.9v3.3h-6.2v-3.3c.1-1.6-.1-3.2-.6-4.7-.3-.9-.7-1.7-1.4-2.4-.5-.5-1.1-.8-1.7-.9-1.1-.1-2.2-.1-3.2 0-.7.1-1.3.4-1.7.9-.7.7-1.2 1.5-1.4 2.4-.5 1.5-.7 3.1-.6 4.7V79c0 3.1.5 5.3 1.5 6.8.3.4.7.8 1.2 1.1 1.7.8 3.6.8 5.2 0 .4-.3.9-.7 1.2-1.1 1-1.4 1.4-3.6 1.4-6.8v-3.3h6.2V79c0 4.6-.9 8.3-2.8 10.9-.9 1.1-1.9 2.1-3.1 2.8-1.7 1-3.6 1.4-5.5 1.3zm39.5-45.8H246v45.7h-6.2V48.2h-10.6v-6.8h27.4v6.8zm3.3 45.8V41.5h6.1v52.6l-6.1-.1h0zm15.9 0h-6.1V41.4h5.1l11.8 31.8V41.4h6.2V94h-5.2l-11.8-31.8V94zm31.4 0c-1.9.1-3.9-.4-5.5-1.3-1.2-.7-2.3-1.7-3.1-2.8-1.9-2.5-2.8-6.2-2.8-10.9V56.4c0-4.7.9-8.4 2.8-10.9 2-2.7 4.8-4 8.7-4s6.7 1.3 8.8 4c1.9 2.5 2.8 6.1 2.8 10.9v3.3h-6.2v-3.3c0-1.5-.1-3.1-.5-4.5a5.45 5.45 0 0 0-1.2-2.4c-.5-.5-1.1-.8-1.7-1-.6-.1-1.3-.2-1.9-.2s-1.3.1-1.9.2c-.7.2-1.3.5-1.7 1a5.45 5.45 0 0 0-1.2 2.4c-.4 1.5-.5 3-.5 4.5v22.7c-.1 1.6.1 3.3.6 4.8.3.9.7 1.7 1.4 2.4.5.5 1.1.8 1.7.9 1.1.1 2.2.1 3.2 0 .7-.1 1.3-.4 1.7-.9a6.08 6.08 0 0 0 1.5-2.4c.5-1.6.7-3.2.6-4.8V75h-7.5v-6.7H319v10.8c0 4.7-.9 8.4-2.8 10.9-.9 1.1-1.9 2.1-3.1 2.8-2 .9-3.9 1.3-5.9 1.2zm-249-74.2l.9 128h1.3l.9-128c1.2-3.8 1.9-7.8 2-11.8 0-4.4-1.9-7.8-3.6-7.8S56.1 3.7 56.1 8c.2 4 .9 8 2.1 11.8z' fill='%23d01f3c'/><path d='M4.4 146.4H0v-24.8h4.4v10.1h40.8v4.6H4.4v10.1zM0 22.7V0h4.4v18.1h14.2V0H23v18.1h22.2v4.5H0v.1z'/><path d='M41.2 50c0 3.5-.8 6.9-2.5 10a15.42 15.42 0 0 1-2.7 3.6l-.1.1c-3.5 3.5-8.2 5.5-13.1 5.3-3.3 0-6.5-.8-9.4-2.4-1.4-.8-2.6-1.8-3.7-2.9-1-1.1-1.9-2.4-2.7-3.7 0-.1-.1-.1-.1-.2-1.6-2.9-2.5-6.2-2.5-9.6-.1-3 .6-5.9 1.9-8.6s3.2-5 5.5-6.8l-2.7-3.4c-2.8 2.1-5.1 4.8-6.7 8C.8 42.8 0 46.5 0 50.3-.1 55 1.2 59.7 3.8 63.7a40.31 40.31 0 0 0 2.6 3.2l.4.4c4.1 4.1 9.7 6.4 15.5 6.2a23.11 23.11 0 0 0 15.3-5.4c.3-.3.6-.5.9-.8a26.04 26.04 0 0 0 3-3.6c2.5-4 3.8-8.7 3.6-13.4.1-3.7-.7-7.4-2.2-10.8-1.5-3.1-3.6-5.9-6.3-8L34 34.9c4.6 3.7 7.3 9.2 7.2 15.1z' fill='%23d01f3c'/><path d='M0 99.4v-4.8l43.3-18.5v4.7L28.8 87v20.1l14.5 6.2v4.7L0 99.4zm24.9-10.8L4.9 97l19.8 8.5.2-16.9h0zM91.8.3h-20v4.1h7.9v31.1h4.4V4.4h7.7zm11.1 0L87.7 35.5h4.8L97.4 24h15.2l5 11.5h4.8L107.4.3h-4.5zm8.1 19.6H99.3L105.2 6l5.8 13.9zM144.6.3l-15 18.7V.3h-4.4l.1 35.2h4.4v-9.7l3.9-4.8 11.2 14.5h5.4l-13.9-18.1L150.2.3zm7.9 0h4.4v35.2h-4.4zm40.6.4l-14.5 34h4.6l4.7-11.1h14.8l4.7 11h4.6L197.4.6h-4.3v.1zm7.8 18.9h-11.3l5.7-13.3 5.6 13.3zM219 .7h-3.8v.5h-.5v33.5h18v-4H219zm31.9 17.2c-1.8-1.6-3.6-2.1-5.6-2.7l-.9-.3c-3.6-1-6.2-2-6.2-5.4 0-2.8 2.3-5.2 5.2-5.2 1.4 0 2.8.5 3.8 1.5s1.6 2.3 1.7 3.8v.5h4.5v-.7c-.1-5.2-4.4-9.4-9.6-9.3-5.6 0-9.9 4-9.9 9.4 0 2.5 1 4.9 2.9 6.5 1.8 1.3 3.9 2.2 6 2.6a14.27 14.27 0 0 1 4.9 2.1c1.3 1.1 2.1 2.7 2.1 4.3v.2c-.1 3.4-3 6-6.3 5.9-3.4 0-6.1-2.8-6.2-6.2v-.5l-4.5.1v.5c0 5.9 4.6 10.4 10.8 10.4h.1c5.7 0 10.3-4.4 10.5-10.2.1-2.8-1.1-5.4-3.3-7.3zM274.3.2h-.3c-4.7 0-9.1 1.8-12.5 5.1s-5.4 7.8-5.4 12.6c0 9.4 8.2 17.4 17.9 17.4h0c9.8 0 17.8-7.9 17.9-17.7h0C291.7 8 283.9.4 274.3.2zm13.4 17.4c0 7.4-6.1 13.5-13.6 13.5h0c-7.4 0-13.7-6.1-13.7-13.3a13.56 13.56 0 0 1 13.2-13.4h.5c3.5 0 6.8 1.3 9.3 3.7 2.6 2.5 4.2 5.9 4.3 9.5zm27-15C311.9.9 308.9.7 305.6.7h-8.7l-.6.2v33.8h4.3V21.5h5.8c3 0 6.2-.2 8.9-2.2 2.6-1.9 4-4.9 3.9-8.1.2-3.5-1.5-6.7-4.5-8.6zm-14 2.1h3.8 1c1.9 0 3.8-.1 5.6.7 2.4.9 3.9 3.1 3.8 5.7 0 2.1-1 4-2.7 5.1-1.9 1.1-4.1 1.2-6.4 1.2h-5.2V4.7h.1zm-13.4 214.7v-.5s2.5-.1 7-3.1l.3.4c-4.5 3.6-7.2 3.2-7.3 3.2z'/><path d='M32.2 169.9c.5-14.3-26.5 1.3-19.3 6.7 1.9 0 1.1-1.5 1.4-2.7.1-.9 3.6-5 4.1-5.3 13.9-9.4 10.3 4.6 3.7 11.3-4.8 5.4-10.6 11.5-17.3 13.9.9 4.1 9.5-.1 13.6 2.5 3.4 1.1 8.1 1.6 10.2-2 .6-.6 1.7-2.2.8-2.8-4.8 6.2-9.8.8-15.7.7-1.6.3-1.4 0-.1-.4 1-.9 3.2-2.3 3.7-2.5 1.8-1.6 3.9-3.2 5.7-4.7 4.4-3.9 8.9-8.3 9.2-14.7zm21.3 6.9c2.5-14.4-9.2-10.2-5.5-8.2 8.9-4.5 1.1 18.3-1.9 20.7-1 2.9-7.2 7-9.3 3-2.6-8.9 0-13.5 0-13.5 2.4-5.4 5.5-11.9 12-13.4.8-.3 3.1 1.3 2.8-.1-12-9-24.4 19.5-17.9 28.4 9.7 7.7 18.3-9.2 19.8-16.9zm29.2 11.8c-2 .5-7.7 8.4-7.1 3.8l.3-1c2.6-11.3-.3-11.1-8.1-4 0 0-1.3 2.1-2.3 2.5.3-3.3 2.2-6.9 2.9-10.3 6.4-1.2 16.2-17.5 5.6-14.4-4.6 3.4-6.9 8.9-8.9 14.1 0 .2-4.2.8-5 .6.3-1.4 1.4-4.5 2-5.6.5-1.6 2.5-3.5 2.1-5.1-4-.2-6.9 8.1-8.1 11-1 .5-4.2 0-1.8 1.6.6 0 1.7 0 1.1.8-.7 3.3-2.2 6.7-1.9 10.3-.2 3.8 1.4 7.4 4.1 9.1.7.5 2.7 1.2 2.5-.3-.2-.6-.8-.5-1.3-.5-4.3-2.6-2.2-8.9-1.2-12.9.8-2.9.9-7.3 3.4-7.2.6 0 2.6 0 3.4-.3-1.7 4.8-2.6 10-4.1 14.8.2 1.3 1.7 1.2 2.6.9 1.2-1.9 3.2-4.6 3.3-4.6 1.5-2.1 5.6-5.9 5.7-6.2.5-.3.9-.8 1.3-.9 0 2.9-1.7 5.7-1.3 8.8-.2 3.1 2.5 3.1 5.1 1.3s6.1-3.7 5.9-6.2l-.2-.1zm-12.8-12.1c1.3-2.9 5-8.4 5-8.4.3-.5.9-.7 1.3-1.1 3.2-.3-.8 6.3-1.7 7.4-1.7 1.7-3.5 3.2-5.7 4.1.2-.7.6-1.4 1.1-2zm66.5-7.8c.4-2.1 1.7-4.4 2.1-6.2-.4-1.6-2.4-3.6-3.7-1.8l-12.4 17.7c-1.7-1.2-2.9-2.8-4.8-3.6-6.3-3.5-17 .8-13.3 9.1 1.7 2.7 5.9 3.1 8.4 1.5 1.8-.8 2.3-4.9-.4-3.5-.3.3-.3 1 .1 1.3.6 2.2-4 1.9-5 .9-1.7-2.1-1-4.1.2-6.3.7-1 3.1-2.6 8.5-1.2 2 .2 4 2 5.4 3.3-4.2 5-8.4 11.8-14.9 14-4.6 1.7-8.8-.5-9-5.5.6-3.4 2.5-1.6 3.7-3.2-3.3-2.9-7.1 4-5.5 7.1 1.5 5.4 8.8 5.3 13.1 3.8 6.1-2.5 9-8.4 12.9-13.1.8-2.1 1.9-1.2 2.6.5 2.6 3.4 4.3 7.9 4.7 12.3.2 1.5 3.8.3 3.7-.9-.6-2.5 2-20.4 3.6-26.2zm-3-.3c-2.2 5.6-3.2 12.4-3.8 18.1-.2 1.2-1.8-2-2.1-2.4-.6-1.3-3-3.2-3-4.7.1-.6.3-.6.6-1.1s3.2-5.2 4.3-6.8c1-1.1 2.5-5.9 4.2-4 0 .3-.1.6-.2.9zm59.5 6.4c1.8-.1 4.9-6 2-6.4-.6 0-.5.5-.9.9-1.3.9-1.4 1.5-1.2 3 .2.7-.9 2.1.1 2.5zm115.5 6c-.9 2-9.3 14.1-11.5 11.9-.2-1.8 3.8-8.8 3.2-10.6-.4-.6-1.4-1.4-2.2-.3 0 .1-4.7 5.2-6.8 7.8-1.4 1.5-6.1 7.3-6.7 2.8-.5-2.6 3-5.4 1.2-7.7-.7-.7-3.7-.9-3.1-2.3.2-.7 1.8-1.8.6-2.4-1.5-1.3-2.8 1.3-2.7 2.6 0 .8.2 2-.6 2.4-.7 1.2-10.1 10.8-11 8.8 0-1.9 1.1-3.7 1.6-5.4.6-1.8-1.5-3.6-3-2.1-1 .6-1.4 2.2-2.3 3-13.9 13.6-.3-11.9 6-4.8-1.3 1.8 1.4 2.7 1.3.8-.8-5.2-10-4.1-12.3-.5-.3.3-2.4 2.6-3 3.6-1.1 2-2.8 2.7-4.6 3.2.4-2.7.9-5.3 1.1-8.1.5-1.1 1.6-2.1.9-3.4-.2-.1-.5-.2-.7-.3-4.3 1.6-6.4 9.1-11.6 8.5-1 .4-1 1.8-.6 2.7-1.1 1.3-5 5.2-5.3 1.4-.5-2.6 3-5.4 1.2-7.7-.7-.7-3.7-.9-3.1-2.3.2-.7 1.8-1.8.6-2.4-1.5-1.3-2.8 1.3-2.7 2.6 0 .8.2 2-.6 2.4-.6 1-3.9 4.6-4.5 4.7-2.2 1.6-7.6 7.4-9.4 3.3 0-1.3-.2-3 1.5-3 12.3-5.5 1.4-14.5-4.5-1.1 0-.1-4.2 2.6-5.2-1.2-.2-1.7 1-3.2.8-4.9.4-2-2.7-2.4-3.5-.8.8 6.4-2.4 10.1-2.5 10.1-5.4 5.3-2.4-2.3-2.2-5.3.2-1.5 2.4-4.8 0-5.4-1.8-.6-2.6 1.3-3.7 2.1-1.4 1.8-12.8 16-11.6 9 .4-1.6.8-2.2 1.1-3.4.9-2.3 4.7-7.5 0-7.4-2 2.4-4.4 5.2-6.1 7.9-6.7 8.3-6.7 2.6-5.4-4.4-.8-8-8 1.1-10.1 3.5 5.8-18.5-4.5-1.3-9.8 2.8-5.4 6.6-4.3-2.8-3.5-6.3-.8-8-8 1.1-10.1 3.5 4.9-15.7-1.7-5.7-6.9 0-.4.2-.5.7-.1 1 2.1-.1 3.3-3.7 4.9-4.5-.2 1.5-5.7 11.5-2.4 11 1.7 0 1.9-.6 2.8-2.4 1.3-2.4 9.8-15.5 7.6-5.4-1 16.2 10.7 1.6 15.4-3.2-.2 1.5-5.7 11.5-2.4 11 1.7 0 3-.4 2.8-2.4 1.2-2.1 2.8-4.5 4.4-6.3 2.2-3.4 4.7-4.7 3.2.9v4.4c.6 5.3 6.1 2.3 8.4-.3 1.1-.9 4.3-4.9 4.3-4.6-5.6 12.9 5.6 7.3 10.1 1.4l4.2-4.9h.1l.1.1c-5.7 13.7 2.8 15.2 9.4 4 .9 2.3 3.8 3.3 5.6 1.2-2.4 8.6 7.5 6.7 11.4 2.3.8-.5 5.5-4.7 7.2-6.5 0-.2.2-.1.3 0 1.9.7-.7 3.4-.5 4.8-1.6 7.8 5.9 5.9 9.2 1.7 1.2 4.4 8.4 5.3 10 .9 1.5-.3 2.7-.9 3.9-1.8-1.7 9.7 7.3 1.5 10.2-1.1h.1c-3.8 9.6 6.7 4.8 10.5.1 0 0 2.6-2.9 4.4-4.6 0-.2.2-.1.3 0 1.3.3.4 1.8.1 2.6-3.3 9.4 4.1 9.4 9.1 3.5.8-1.1 4.1-4.7 5.1-5.6 0 .1.2.1.2.1-6.9 14.8 1 11 8.5 3.3-3.5 9.9-6.6 25.4-17.7 28.9-1.2.2-2.5-2.3-3-.2 8.6 5 18.7-10.7 20.4-18 1.9-4.6 6.5-16.2 5.7-17.4.4-.8-.5-2.4-1.5-1.9zm-88.1 4.8c5.3-6.2 2.6 3.9-1.8 3.2 0-.9 1.2-2.7 1.8-3.2zm27.4 8.7c-1.2.6-3.4 0-3.6-1.4.1-.1.4-.7.4-.8.8.3 3.2 1.2 3.9 1.2-.1.4-.4.8-.7 1zm2.6-8.5c-.1 1.3-1 5.2-1.2 6.1-1.4.6-3-.1-4.2-.8.2-2.5 3.4-4.5 5.2-6.3.1 0 .4-.5.5-.5l-.3 1.5z' fill='%23fff'/><defs ><path id='B' d='M49.6 228.3c-5.4-.1-9.8-4.5-9.7-9.9s4.5-9.8 9.9-9.7 9.8 4.5 9.7 9.9-4.5 9.7-9.9 9.7zm0-17.1c-4.9 0-7.1 4.3-7.1 7.3.1 3.9 3.4 7 7.3 6.9s7-3.4 6.9-7.3c-.1-3.8-3.3-6.9-7.1-6.9z'/><path id='C' d='M127.1 213.8c-2.3-3-6.6-3.6-9.6-1.3-1.8 1.4-2.8 3.6-2.7 5.9-.1 4 3 7.2 7 7.4 2 0 3.9-1 5.2-2.5h3.4c-2.7 4.7-8.7 6.3-13.4 3.6s-6.3-8.7-3.6-13.4c1.7-3 5-4.9 8.5-4.9 2.1 0 4.1.7 5.8 1.8 1.2.9 2.2 2 2.9 3.4h-3.5z'/><path id='D' d='M215.7 227.9l-6.3-8.8h-.1v8.8h-2.8v-19h5.9c2.7 0 4 .6 5 1.6 1.1 1.2 1.7 2.8 1.7 4.5.2 3-2.1 5.6-5.1 5.8h-.3l5.3 7.1h-3.3zm-6.3-9.1h1.9c1.1 0 5.2-.1 5.2-3.7 0-1.6-1.1-3.5-4.2-3.5h-2.9v7.2z'/></defs></svg>",
                    "image/svg+xml",
                    Array.Empty<NiftyFile>(),
                    new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    "1.0",
                    new[]{
                        new KeyValuePair<string,string>("Organization", "Taki Alsop Conducting Fellowship"),
                        new KeyValuePair<string,string>("Series", "TACF Global Concert Series")}))
            .ToArray();

        var collection = new NiftyCollection(
            Id: fakeCollectionId,
            PolicyId: "19bb7dd38a3ef5ddbe646d36bd376ebed20ef370c4eb81d9b08e6b33",// "b1e785ba20061e1320693dce777c9939eea8cfae74a9120c844b8b1b",
            Name: "TACF Global Concert Series",
            Description: "125 ADA, 115 NFTs, 23th July 2023 Expiry",
            IsActive: true,
            Publishers: new[] { "takialsop.org", "mintsafe.io" },
            BrandImage: "",
            CreatedAt: new DateTime(2022, 01, 06, 0, 0, 0, DateTimeKind.Utc),
            LockedAt: new DateTime(2023, 7, 23, 0, 0, 0, DateTimeKind.Utc),
            SlotExpiry: 96997186, //98504109
            Royalty: new Royalty(0.08, "addr1q9kjqwrp8fy5ea8qlac6dlecz2dc4grchm8q6rgvv4gdup9zflwyjvg8uhpyfhh46khtt90cz4wzsvqqc44cs4qmyx6qts4he6"));

        var activeSales = collection.IsActive && IsSaleOpen(sale) ? new[] { sale } : Array.Empty<Sale>();

        return Task.FromResult(new CollectionAggregate(collection, tokens, ActiveSales: activeSales));
    }

    public Task InsertCollectionAggregateAsync(CollectionAggregate collectionAggregate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private List<JsonNode?> LoadDynamicJsonFromDirAsync(string path)
    {
        var files = Directory.GetFiles(path);
        var list = new List<JsonNode?>();
        foreach (var filePath in files)
        {
            var raw = File.ReadAllText(filePath);
            var model = JsonSerializer.Deserialize<JsonNode>(raw);
            list.Add(model);
        }

        return list.Where(x => x != null).ToList();
    }

    private static bool IsSaleOpen(Sale sale)
    {
        if (!sale.IsActive
            || (sale.Start > DateTime.UtcNow)
            || (sale.End.HasValue && sale.End < DateTime.UtcNow))
            return false;

        return true;
    }
}
