using FluentAssertions;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Mintsafe.Lib.UnitTests;

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
        ulong lovelaceQuantity, params string[] customTokenUnits)
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
        ulong lovelaceQuantityLhs, ulong lovelaceQuantityRhs, ulong expectedLovelaceValues)
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
        ulong ftQuantityLhs, ulong ftQuantityRhs, ulong expectedFtQuantity)
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
        ulong lovelaceValue, ulong expectedMinUtxo)
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
        ulong expectedMinUtxoLovelace, params string[] customTokenUnits)
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
        ulong expectedMinUtxoLovelace, params string[] customTokenUnits)
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
        ulong expectedMinUtxoLovelace, int count, string customTokenUnitBase)
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

    [Fact]
    public void DeriveMinUtxoLovelace_For_Token_Bundle_When_Output_Has_Two_Assets_Under_The_Same_Policy()
    {
        var values = new List<Value> { new Value(Assets.LovelaceUnit, 100_000000) };
        values.Add(new Value("e8a447d4e19016ca2aa74d20b4c4de87adb1f21dfb5493bf2d7281a6.COND1", 20));
        values.Add(new Value("e8a447d4e19016ca2aa74d20b4c4de87adb1f21dfb5493bf2d7281a6.COND2", 20));

        var bundle = new AggregateValue(100_000000, new[]
        {
            new NativeAssetValue("e8a447d4e19016ca2aa74d20b4c4de87adb1f21dfb5493bf2d7281a6", "434f4e4431", 20),
            new NativeAssetValue("e8a447d4e19016ca2aa74d20b4c4de87adb1f21dfb5493bf2d7281a6", "434f4e4432", 20),
        });

        var minUtxoOld = TxUtils.CalculateMinUtxoLovelace(values.ToArray());
        var minUtxoNew = TxUtils.CalculateMinUtxoLovelace(bundle);

        minUtxoOld.Should().Be(1413762);
        minUtxoNew.Should().Be(1413762);
    }

    [Theory]
    [InlineData(1_000000UL, 10_000000UL)]
    [InlineData(81_590452UL, 6032_591752UL)]
    [InlineData(33_362_564_961_123456UL, 9362_564_961_394103UL)]
    public void Consolidate_Output_Values_Calculating_Lovelaces_Correctly_When_No_Native_Assets_Exist(params ulong[] lovelaceValues)
    {
        var outputValues = lovelaceValues
            .Select(lv => new AggregateValue(lv, Array.Empty<NativeAssetValue>())).ToArray();

        var foldedOutputValue = outputValues.Sum();

        // No Sum extension method for ulong types so casting to long is required
        foldedOutputValue.Lovelaces.Should().Be((ulong)lovelaceValues.Sum(lv => (long)lv)); 
    }

    
    [Theory]
    [InlineData(
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e646157545030313437543139\", \"Quantity\": 1}]}")]
    [InlineData(
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e646157545030313437543139\", \"Quantity\": 1}]}",
        "{\"Lovelaces\": 10000000, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133355431\", \"Quantity\": 1},{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"Quantity\": 1}]}")]
    [InlineData(
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e646157545030313437543139\", \"Quantity\": 1}]}",
        "{\"Lovelaces\": 10000000, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133355431\", \"Quantity\": 1}]}",
        "{\"Lovelaces\": 10000000, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133345437\", \"Quantity\": 1}]}",
        "{\"Lovelaces\": 10000000, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133345438\", \"Quantity\": 1}]}")]
    public void Consolidate_Output_Values_Combining_Native_Assets_Correctly_When_Output_Values_Have_Distinct_Native_Assets(params string[] outputJson)
    {
        var outputValues = outputJson
            .Select(json => JsonSerializer.Deserialize<OutputValueDto>(json))
            .Select(ov => new AggregateValue(ov.Lovelaces, ov.NativeAssets.Select(na => new NativeAssetValue(na.PolicyId, na.AssetNameHex, na.Quantity)).ToArray())).ToArray();

        var actualOutputValue = outputValues.Sum();

        actualOutputValue.Lovelaces.Should().Be((ulong)outputValues.Sum(ov => (long)ov.Lovelaces));
        actualOutputValue.NativeAssets.Length.Should().Be(outputValues.SelectMany(ov => ov.NativeAssets).ToArray().Length);
    }

    [Theory]
    [InlineData(
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"da8c30857834c6ae7203935b89278c532b3995245295456f993e1d24\", \"AssetNameHex\": \"4c51\", \"Quantity\": 1243}]}",
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"da8c30857834c6ae7203935b89278c532b3995245295456f993e1d24\", \"AssetNameHex\": \"4c51\", \"Quantity\": 1243}]}")]
    [InlineData(
        "{\"Lovelaces\": 106547581, \"NativeAssets\": [{\"PolicyId\": \"f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add\", \"AssetNameHex\": \"566572697472656552617265\", \"Quantity\": 5005},{\"PolicyId\": \"da8c30857834c6ae7203935b89278c532b3995245295456f993e1d24\", \"AssetNameHex\": \"4c51\", \"Quantity\": 288}]}",
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add\", \"AssetNameHex\": \"566572697472656552617265\", \"Quantity\": 5005},{\"PolicyId\": \"da8c30857834c6ae7203935b89278c532b3995245295456f993e1d24\", \"AssetNameHex\": \"4c51\", \"Quantity\": 32}]}",
        "{\"Lovelaces\": 24957129, \"NativeAssets\": [{\"PolicyId\": \"da8c30857834c6ae7203935b89278c532b3995245295456f993e1d24\", \"AssetNameHex\": \"4c51\", \"Quantity\": 256}]}")]
    [InlineData(
        "{\"Lovelaces\":1525677267,\"NativeAssets\": [{\"PolicyId\": \"f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add\", \"AssetNameHex\": \"566572697472656552617265\", \"Quantity\": 579},{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133355431\", \"Quantity\": 1},{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133345437\", \"Quantity\": 1},{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133345438\", \"Quantity\": 1}]}",
        "{\"Lovelaces\": 81590452, \"NativeAssets\": [{\"PolicyId\": \"f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add\", \"AssetNameHex\": \"566572697472656552617265\", \"Quantity\": 123}]}",
        "{\"Lovelaces\": 10000000, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133355431\", \"Quantity\": 1}]}",
        "{\"Lovelaces\":553947658, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133345437\", \"Quantity\": 1},{\"PolicyId\": \"f7c777fdd4531cf1c477551360e45b9684073c05c2fa61334f8f9add\", \"AssetNameHex\": \"566572697472656552617265\", \"Quantity\": 456}]}",
        "{\"Lovelaces\":880139157, \"NativeAssets\": [{\"PolicyId\": \"a1c74e98c9a6618deed1e39f74062588ef2c43a19eaa2f11a1eda733\", \"AssetNameHex\": \"4a6f736570684d6972616e6461575450303133345438\", \"Quantity\": 1}]}")]
    public void Consolidate_Output_Values_Combining_Native_Assets_Correctly_When_Output_Values_Have_Overlapping_Assets(
        string expectedOutputJson,
        params string[] outputValuesJson)
    {
        var outputValues = outputValuesJson
            .Select(json => JsonSerializer.Deserialize<OutputValueDto>(json))
            .Select(ov => new AggregateValue(ov.Lovelaces, ov.NativeAssets.Select(na => new NativeAssetValue(na.PolicyId, na.AssetNameHex, na.Quantity)).ToArray())).ToArray();

        var actualOutputValue = outputValues.Sum();

        var expectedFoldedOutputValue = JsonSerializer.Deserialize<OutputValueDto>(expectedOutputJson);
        actualOutputValue.Lovelaces.Should().Be(expectedFoldedOutputValue.Lovelaces);
        actualOutputValue.NativeAssets.Length.Should().Be(expectedFoldedOutputValue.NativeAssets.Length);
        outputValues.SelectMany(ov => ov.NativeAssets)
            .All(na => actualOutputValue.NativeAssets.Count(
                resultNativeAssets => resultNativeAssets.PolicyId == na.PolicyId && resultNativeAssets.AssetName== na.AssetName) == 1)
            .Should().BeTrue();
        expectedFoldedOutputValue.NativeAssets
            .All(ena => actualOutputValue.NativeAssets.Count(
                ana => ana.PolicyId == ena.PolicyId && ana.AssetName== ena.AssetNameHex && ana.Quantity == ena.Quantity) == 1);
    }
}

public record OutputValueDto
{
    public ulong Lovelaces { get; set; }
    public NativeAssetValueDto[]? NativeAssets { get; set; }
}

public record NativeAssetValueDto
{
    public string? PolicyId { get; set; }
    public string? AssetNameHex { get; set; }
    public ulong Quantity { get; set; }
}