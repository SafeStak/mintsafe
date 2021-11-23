using FluentAssertions;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Mintsafe.Lib.UnitTests
{
    public class TxUtilsShould
    {
        const long AdaOnlyUtxoLovelaces = 999978;

        [Theory]
        [InlineData(
            8822996633231, "e9b6f907ea790ca51957eb513430eb0ec155f8df654d48e961d7ea3e.cryptodingos00002")]
        [InlineData(
            1620654, 
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.cryptoroos123",
            "91acca0a2614212d68a5ae7313c85962849994aab54e340d3a68aabb.cryptoquokka99999")]
        [InlineData(
            422810293, 
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.cryptokookaburras0123",
            "91acca0a2614212d68a5ae7313c85962849994aab54e340d3a68aabb.cryptokoalas0087",
            "e8209a96a456202276f66224241a703676122d606d208fe464f2e09f.cryptowombats27699")]
        public void SubtractValues_With_No_Effect_When_Rhs_Values_Are_Empty(
            long lovelaceQuantity, params string[] customTokenUnits)
        {
            var valuesLhs = new List<Value> { new Value(Assets.LovelaceUnit, lovelaceQuantity) };
            valuesLhs.AddRange(customTokenUnits.Select(u => new Value(u, 1)));
            var valuesRhs = Array.Empty<Value>();

            var result = TxUtils.SubtractValues(valuesLhs.ToArray(), valuesRhs);

            result.Length.Should().Be(customTokenUnits.Length + 1); // + 1 for Default Lovelace Unit
            result.First().Unit.Should().Be(Assets.LovelaceUnit);
            result.First().Quantity.Should().Be(lovelaceQuantity);
            var valueLookup = result.ToDictionary(v => v.Unit, v => v.Quantity);
            var customTokens = result.Where(v => v.Unit != Assets.LovelaceUnit).ToArray();
            customTokens.Length.Should().Be(customTokenUnits.Length);
            foreach (var value in customTokens)
            {
                valueLookup[value.Unit].Should().Be(1);
            }
        }

        [Theory]
        [InlineData(2993600, 179097, 2814503)]
        [InlineData(15000000, 1413762, 13586238)]
        [InlineData(14946_734549, 2299_663323, 12647071226)]
        public void SubtractValues_Correctly_When_Only_Ada_Values(
            long lovelaceQuantityLhs, long lovelaceQuantityRhs, long expectedLovelaceValues)
        {
            var valuesLhs = new[] { new Value(Assets.LovelaceUnit, lovelaceQuantityLhs) };
            var valuesRhs = new[] { new Value(Assets.LovelaceUnit, lovelaceQuantityRhs) };

            var result = TxUtils.SubtractValues(valuesLhs, valuesRhs);
            result.Length.Should().Be(1);
            result.First().Unit.Should().Be(Assets.LovelaceUnit);
            result.First().Quantity.Should().Be(expectedLovelaceValues);
        }

        [Theory]
        [InlineData(761792, 43284, 718508)]
        [InlineData(9014946734549, 8822996633231, 191950101318)]
        public void SubtractValues_Correctly_When_Ada_And_Custom_Fungible_Token_Values_Are_Given(
            long ftQuantityLhs, long ftQuantityRhs, long expectedFtQuantity)
        {
            var valuesLhs = new[] { 
                new Value(Assets.LovelaceUnit, 15000000),
                new Value("f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add.ft", ftQuantityLhs),
                new Value("7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.nft0001", 1),
                new Value("7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.nft9999", 1),
            };
            var valuesRhs = new[] { 
                new Value(Assets.LovelaceUnit, 1413762),
                new Value("f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add.ft", ftQuantityRhs),
            };

            var result = TxUtils.SubtractValues(valuesLhs, valuesRhs);

            result.Length.Should().Be(4);
            var valueLookup = result.ToDictionary(v => v.Unit, v => v.Quantity);
            valueLookup[Assets.LovelaceUnit].Should().Be(13586238);
            valueLookup["f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add.ft"].Should().Be(expectedFtQuantity);
            valueLookup["7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.nft0001"].Should().Be(1);
            valueLookup["7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.nft9999"].Should().Be(1);
        }

        [Theory]
        [InlineData(0, AdaOnlyUtxoLovelaces)]
        [InlineData(1000000, AdaOnlyUtxoLovelaces)]
        [InlineData(821_945678, AdaOnlyUtxoLovelaces)]
        [InlineData(5197820531_945678, AdaOnlyUtxoLovelaces)]
        public void DeriveMinUtxoLovelace_Correctly_Given_All_Default_Params_When_Values_Have_Ada_Only(
            long lovelaceValue, long expectedMinUtxo)
        {
            var values = new[] { new Value(Assets.LovelaceUnit, lovelaceValue) };

            var minUtxo = TxUtils.CalculateMinUtxoLovelace(values);

            minUtxo.Should().Be(expectedMinUtxo);
        }

        [Theory]
        [InlineData(
            1413762, "e9b6f907ea790ca51957eb513430eb0ec155f8df654d48e961d7ea3e.cryptoquokkas00002")]
        [InlineData(
            1620654, "0a85dd1543465407852c90e66c074a3b52ea2d7c77a2346ddc20550a.cryptoroos123",
            "91acca0a2614212d68a5ae7313c85962849994aab54e340d3a68aabb.cryptopossums99999")]
        public void DeriveMinUtxoLovelace_Given_Default_Values_When_Output_Has_Multiple_NFTs_Under_Same_Policy_And_No_Data_Hash(
            long expectedMinUtxoLovelace, params string[] customTokenUnits)
        {
            var values = new List<Value> { new Value(Assets.LovelaceUnit, 100_000000) };
            values.AddRange(customTokenUnits.Select(u => new Value(u, 1)));

            var minUtxo = TxUtils.CalculateMinUtxoLovelace(values.ToArray(), hasDataHash: false);

            minUtxo.Should().Be(expectedMinUtxoLovelace);
        }

        [Theory]
        [InlineData(
            1689618, "89d6e39b026145fdab359296e1c8752960641d041b061bdbe80b9c11.nft1")]
        [InlineData(
            1965474, "5bc031932ddb0e89b880569171da1e0e63c4c07867df8e35214e8213.cryptoemus0001",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.cryptoquokka99999")]
        public void DeriveMinUtxoLovelace_Given_Default_Values_When_Output_Has_Multiple_NFTs_Under_Same_Policy_And_Data_Hash(
            long expectedMinUtxoLovelace, params string[] customTokenUnits)
        {
            var values = new List<Value> { new Value(Assets.LovelaceUnit, 100_000000) };
            values.AddRange(customTokenUnits.Select(u => new Value(u, 1)));

            var minUtxo = TxUtils.CalculateMinUtxoLovelace(values.ToArray(), hasDataHash: true);

            minUtxo.Should().Be(expectedMinUtxoLovelace);
        }

        [Theory]
        [InlineData(
            4_517142, 55, "34d825881c5a6465d0398dbbe301222427d3572f31ba36148e89ce54.cryptoemus")]
        [InlineData(
            47_619642, 888, "5bc031932ddb0e89b880569171da1e0e63c4c07867df8e35214e8213.cryptowallabies")]
        [InlineData(
            518_919618, 10000, "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.cryptokoalas")]
        public void DeriveMinUtxoLovelace_Given_Default_Values_When_Output_Has_Many_NFTs_Under_Same_Policy_And_Data_Hash(
            long expectedMinUtxoLovelace, int count, string customTokenUnitBase)
        {
            var values = new List<Value> { new Value(Assets.LovelaceUnit, 100_000000) };
            values.AddRange(Enumerable.Range(1, count).Select(i => new Value($"{customTokenUnitBase}i", 1)));

            var minUtxo = TxUtils.CalculateMinUtxoLovelace(values.ToArray(), hasDataHash: true);

            minUtxo.Should().Be(expectedMinUtxoLovelace);
        }

        // Verifying https://github.com/ilap/ShelleyStuffs#min-utxo-ada-calculation
        [Theory]
        [InlineData(
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f00.01234567890123456789045678901200",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f01.01234567890123456789045678901201",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f02.01234567890123456789045678901202",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f03.01234567890123456789045678901203",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f04.01234567890123456789045678901204",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f05.01234567890123456789045678901205",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f06.01234567890123456789045678901206",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f07.01234567890123456789045678901207",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f08.01234567890123456789045678901208",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f09.01234567890123456789045678901209",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f10.01234567890123456789045678901210",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f11.01234567890123456789045678901211",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f12.01234567890123456789045678901212",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f13.01234567890123456789045678901213",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f14.01234567890123456789045678901214",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f15.01234567890123456789045678901215",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f16.01234567890123456789045678901216",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f17.01234567890123456789045678901217",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f18.01234567890123456789045678901218",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f19.01234567890123456789045678901219",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f20.01234567890123456789045678901220",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f21.01234567890123456789045678901221",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f22.01234567890123456789045678901222",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f23.01234567890123456789045678901223",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f24.01234567890123456789045678901224",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f25.01234567890123456789045678901225",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f26.01234567890123456789045678901226",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f27.01234567890123456789045678901227",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f28.01234567890123456789045678901228",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f29.01234567890123456789045678901229",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f30.01234567890123456789045678901230",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f31.01234567890123456789045678901231",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f32.01234567890123456789045678901232",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f33.01234567890123456789045678901233",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f34.01234567890123456789045678901234",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f35.01234567890123456789045678901235",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f36.01234567890123456789045678901236",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f37.01234567890123456789045678901237",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f38.01234567890123456789045678901238",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f39.01234567890123456789045678901239",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f40.01234567890123456789045678901240",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f41.01234567890123456789045678901241",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f42.01234567890123456789045678901242",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f43.01234567890123456789045678901243",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f44.01234567890123456789045678901244",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f45.01234567890123456789045678901245",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f46.01234567890123456789045678901246",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f47.01234567890123456789045678901247",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f48.01234567890123456789045678901248",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f49.01234567890123456789045678901249",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f50.01234567890123456789045678901250",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f51.01234567890123456789045678901251",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f52.01234567890123456789045678901252",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f53.01234567890123456789045678901253",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f54.01234567890123456789045678901254",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f55.01234567890123456789045678901255",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f56.01234567890123456789045678901256",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f57.01234567890123456789045678901257",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f58.01234567890123456789045678901258",
            "7cb31677481b1112db5aaa2acdffbe624d8195d416da8b788cb51f59.01234567890123456789045678901259")]
        public void DeriveMinUtxoLovelace_Given_Default_Values_When_Output_Has_60_Distinct_Policies_And_60_Distinct_AssetNames_And_DataHash(
            params string[] units)
        {
            var values = new List<Value> { new Value(Assets.LovelaceUnit, 100_000000) };
            values.AddRange(units.Select(u => new Value(u, 1)));

            var minUtxo = TxUtils.CalculateMinUtxoLovelace(values.ToArray(), hasDataHash: true);

            minUtxo.Should().Be(20103006);
        }
    }
}
