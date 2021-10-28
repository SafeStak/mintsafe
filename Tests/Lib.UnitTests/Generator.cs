using System;
using System.Collections.Generic;
using System.Linq;

namespace NiftyLaunchpad.Lib.UnitTests
{
    public static class Generator
    {
        public static NiftyLaunchpadSettings GenerateSettings()
        {
            return new NiftyLaunchpadSettings(Network.Testnet, 5, "testnetabc");
        }

        public static NiftyCollection GenerateCollection(
            string id = null,
            string policyId = null)
        {
            return new NiftyCollection(
                Id: id == null ? Guid.NewGuid() : Guid.Parse(id),
                PolicyId: policyId ?? "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                Name: "BRIKMAESTRO",
                Description: "Creations from the BRIKAVERSE",
                IsActive: true,
                Publishers: new[] { "BRIKMAESTRO", "NiftyLaunchpad.net" },
                BrandImage: "ipfs://cid",
                CreatedAt: new DateTime(2022, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                LockedAt: new DateTime(2022, 11, 30, 0, 0, 0, DateTimeKind.Utc));
        }

        public static List<Nifty> GenerateTokens(int mintableTokenCount)
        {
            return Enumerable.Range(0, mintableTokenCount)
                .Select(i => new Nifty(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    true,
                    $"Token{i}",
                    $"Token {i}",
                    $"Token {i} Description",
                    new[] { "NiftyLaunchpad.net" },
                    $"ipfs://{i}",
                    "image/png",
                    Array.Empty<NiftyFile>(),
                    DateTime.UtcNow,
                    new Royalty(0, string.Empty),
                    "1.0",
                    new Dictionary<string, string>()))
                .ToList();
        }

    }
}
