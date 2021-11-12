using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mintsafe.Lib.UnitTests
{
    public static class Generator
    {
        public static MintsafeSaleWorkerSettings GenerateSettings()
        {
            return new MintsafeSaleWorkerSettings(
                Network.Testnet, 
                PollingIntervalSeconds: 5, 
                BasePath: "~/nlp/", 
                BlockFrostApiKey: "testnetabc", 
                CollectionId: Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae"));
        }

        public static NiftyCollection GenerateCollection(
            string id = null,
            string policyId = null)
        {
            return new NiftyCollection(
                Id: id == null ? Guid.NewGuid() : Guid.Parse(id),
                PolicyId: policyId ?? "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                Name: "GREATARTIST",
                Description: "Top secret artist",
                IsActive: true,
                Publishers: new[] { "topsecret", "mintsafe.io" },
                BrandImage: "ipfs://cid",
                CreatedAt: new DateTime(2021, 9, 4, 0, 0, 0, DateTimeKind.Utc),
                LockedAt: new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                SlotExpiry: 44674366);
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
                    new[] { "mintsafe.io" },
                    $"ipfs://{i}",
                    "image/png",
                    Array.Empty<NiftyFile>(),
                    DateTime.UtcNow,
                    new Royalty(0, string.Empty),
                    "1.0",
                    new Dictionary<string, string>()))
                .ToList();
        }

        public static Utxo[] GenerateUtxos(int count, params long[] values)
        {
            if (values.Length != count)
                throw new ArgumentException($"{nameof(values)} must be the same length as count", nameof(values));

            return Enumerable.Range(0, count)
                .Select(i => new Utxo(
                    "127745e23b81a5a5e22a409ce17ae8672b234dda7be1f09fc9e3a11906bd3a11",
                    i,
                    new[] { new Value(Assets.LovelaceUnit, values[i]) }))
                .ToArray();
        }

        public static Sale GenerateSale(
            int totalReleaseQuantity = 500,
            int maxAllowedPurchaseQuantity = 10,
            bool isActive = true,
            long lovelacesPerToken = 15000000,
            string proceedsAddress = "addr_test1vzj4c522pr5n6texvcl24kl9enntr4knl4ucecd7pkt24mglna4pz")
        {
            return new Sale(
                Id: Guid.NewGuid(),
                CollectionId: Guid.NewGuid(),
                IsActive: isActive,
                Name: "Preview Launch #1",
                Description: "Limited 100 item launch",
                LovelacesPerToken: lovelacesPerToken,
                SaleAddress: "addr_test1vz0hx28mmdz0ey3pzqe5nxg08urjhzydpvvmcx4v4we5mvg6733n5",
                ProceedsAddress: proceedsAddress,
                TotalReleaseQuantity: totalReleaseQuantity,
                MaxAllowedPurchaseQuantity: maxAllowedPurchaseQuantity);
        }

        public static TxIoAggregate GenerateTxIoAggregate(
            string txHash = "01daae688d236601109d9fc1bc11d7380a7617e6835eddca6527738963a87279",
            string inputAddress = "addr_test1vrfxxeuzqfuknfz4hu0ym4fe4l3axvqd7t5agd6pfzml59q30qc4x",
            long inputLovelaceQuantity = 10200000,
            string outputAddress = "addr_test1vre6wmde3qz7h7eerk98lgtkuzjd5nfqj4wy0fwntymr20qee2cxk",
            long outputLovelaceQuantity = 10000000)
        {
            return new TxIoAggregate(
                txHash,
                Inputs: new[] { new TxIo(inputAddress, 0, new[] { new Value(Assets.LovelaceUnit, inputLovelaceQuantity) }) },
                Outputs: new[] { new TxIo(outputAddress, 0, new[] { new Value(Assets.LovelaceUnit, outputLovelaceQuantity) }) });
        }
    }
}
