using FluentAssertions;
using Mintsafe.Abstractions;
using System;
using Xunit;

namespace Mintsafe.Lib.UnitTests
{
    public class SalePurchaseRequesterShould
    {
        [Theory]
        [InlineData(10000000, 10000000, 1)]
        [InlineData(39000000, 15000000, 2)]
        [InlineData(50000000, 10000000, 5)]
        public void Correctly_Calculate_Quantity(
            long utxoValueLovelace, long costPerTokenLovelace, int expectedQuantity)
        {
            var sale = FakeGenerator.GenerateSale(lovelacesPerToken: costPerTokenLovelace);

            var salePurchase = SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new Value(Assets.LovelaceUnit, utxoValueLovelace) }),
                    sale);

            salePurchase.NiftyQuantityRequested.Should().Be(expectedQuantity);
        }

        [Theory]
        [InlineData(10000000, 10000000, 0)]
        [InlineData(34000001, 34000000, 1)]
        [InlineData(25000000, 10000000, 5000000)]
        public void Correctly_Calculate_Change(
            long utxoValueLovelace, long costPerTokenLovelace, int expectedChange)
        {
            var sale = FakeGenerator.GenerateSale(lovelacesPerToken: costPerTokenLovelace);

            var salePurchase = SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new Value(Assets.LovelaceUnit, utxoValueLovelace) }),
                    sale);

            salePurchase.ChangeInLovelace.Should().Be(expectedChange);
        }

        [Theory]
        [InlineData(1, 10, "9df6598c24f9a44cf1b10c527d670b3c5a9b9d435f81af15a5773b4b267b62e0", "9ef60a89-8a04-40a9-b45d-2174d64f761f")]
        [InlineData(10000, 100000, "9df6598c24f9a44cf1b10c527d670b3c5a9b9d435f81af15a5773b4b267b62e0", "f16d7a02-61f2-4ec3-ba81-b29a5f07a68f")]
        public void Correctly_Maps_Values_When_Sale_Is_Active_And_Within_Start_End_Dates(
            int secondsAfterStart, int secondsBeforeEnd, string txHash, string saleId)
        {
            var sale = FakeGenerator.GenerateSale(
                saleId: saleId, 
                lovelacesPerToken: 10000000,
                start: DateTime.UtcNow.AddSeconds(-secondsAfterStart), 
                end: DateTime.UtcNow.AddSeconds(secondsBeforeEnd));

            var salePurchase = SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        txHash,
                        0,
                        new[] { new Value(Assets.LovelaceUnit, 10000000) }),
                    sale);

            salePurchase.Utxo.TxHash.Should().Be(txHash); // TODO: more field assertions
            salePurchase.SaleId.Should().Be(Guid.Parse(saleId));
        }

        [Theory]
        [InlineData(1500000, 1500001)]
        [InlineData(34999999, 35000000)]
        public void Throws_InsufficientPaymentException_When_Utxo_Value_Is_Less_Than_LovelacesPerToken(
            long utxoValueLovelace, long costPerTokenLovelace)
        {
            var sale = FakeGenerator.GenerateSale(lovelacesPerToken: costPerTokenLovelace);

            Action action = () =>
            {
                SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865", 
                        0, 
                        new[] { new Value(Assets.LovelaceUnit, utxoValueLovelace) }), 
                    sale);
            };

            action.Should().Throw<InsufficientPaymentException>();
        }

        [Fact]
        public void Throws_SaleInactiveException_When_Sale_Is_Inactive()
        {
            var sale = FakeGenerator.GenerateSale(isActive: false);

            Action action = () =>
            {
                SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new Value(Assets.LovelaceUnit, 100000000) }),
                    sale);
            };

            action.Should().Throw<SaleInactiveException>();
        }

        [Theory]
        [InlineData(20000000, 10000000, 1)]
        [InlineData(80000000, 20000000, 3)]
        public void Throws_MaxAllowedPurchaseQuantityExceededException_When_Quantity_Exceeds_Max_Allowed(
            long utxoValueLovelace, long costPerTokenLovelace, int maxAllowedPurchaseQuantity)
        {
            var sale = FakeGenerator.GenerateSale(lovelacesPerToken: costPerTokenLovelace, maxAllowedPurchaseQuantity: maxAllowedPurchaseQuantity);

            Action action = () =>
            {
                SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new Value(Assets.LovelaceUnit, utxoValueLovelace) }),
                    sale);
            };

            action.Should().Throw<MaxAllowedPurchaseQuantityExceededException>();
        }

        [Theory]
        [InlineData(10)]
        [InlineData(3600)]
        public void Throws_SalePeriodOutOfRangeException_When_Sale_Has_Not_Started(
            int secondsInTheFuture)
        {
            var sale = FakeGenerator.GenerateSale(start: DateTime.UtcNow.AddSeconds(secondsInTheFuture));

            Action action = () =>
            {
                SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new Value(Assets.LovelaceUnit, 100000000) }),
                    sale);
            };

            action.Should().Throw<SalePeriodOutOfRangeException>();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3600)]
        public void Throws_SalePeriodOutOfRangeException_When_Sale_Has_Already_Ended(
            int secondsInThePast)
        {
            var sale = FakeGenerator.GenerateSale(end: DateTime.UtcNow.AddSeconds(-secondsInThePast));

            Action action = () =>
            {
                SalePurchaseGenerator.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new Value(Assets.LovelaceUnit, 100000000) }),
                    sale);
            };

            action.Should().Throw<SalePeriodOutOfRangeException>();
        }
    }
}
