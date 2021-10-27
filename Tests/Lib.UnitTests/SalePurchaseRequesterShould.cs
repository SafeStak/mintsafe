using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NiftyLaunchpad.Lib.UnitTests
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
            var sale = GenerateSalePeriod(costPerTokenLovelace: costPerTokenLovelace);

            var salePurchase = SalePurchaseRequester.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new UtxoValue("lovelace", utxoValueLovelace) }),
                    sale);

            salePurchase.NiftyQuantityRequested.Should().Be(expectedQuantity);
        }

        [Theory]
        [InlineData(10000000, 10000000, 0)]
        [InlineData(25000000, 10000000, 5000000)]
        public void Correctly_Calculate_Change(
            long utxoValueLovelace, long costPerTokenLovelace, int expectedChange)
        {
            var sale = GenerateSalePeriod(costPerTokenLovelace: costPerTokenLovelace);

            var salePurchase = SalePurchaseRequester.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new UtxoValue("lovelace", utxoValueLovelace) }),
                    sale);

            salePurchase.ChangeInLovelace.Should().Be(expectedChange);
        }

        [Theory]
        [InlineData(1500000, 1510000)]
        [InlineData(10000000, 15000000)]
        public void Throws_InsufficientPaymentException_When_Utxo_Value_Is_Less_Than_LovelacesPerToken(
            long utxoValueLovelace, long costPerTokenLovelace)
        {
            var sale = GenerateSalePeriod(costPerTokenLovelace: costPerTokenLovelace);

            Action action = () =>
            {
                SalePurchaseRequester.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865", 
                        0, 
                        new[] { new UtxoValue("lovelace", utxoValueLovelace) }), 
                    sale);
            };

            action.Should().Throw<InsufficientPaymentException>();
        }

        [Theory]
        [InlineData(20000000, 10000000, 1)]
        [InlineData(80000000, 20000000, 3)]
        public void Throws_MaxAllowedPurchaseQuantityExceededException_When_Quantity_Exceeds_Max_Allowed(
            long utxoValueLovelace, long costPerTokenLovelace, int maxAllowedPurchaseQuantity)
        {
            var sale = GenerateSalePeriod(costPerTokenLovelace: costPerTokenLovelace, maxAllowedPurchaseQuantity: maxAllowedPurchaseQuantity);

            Action action = () =>
            {
                SalePurchaseRequester.FromUtxo(
                    new Utxo(
                        "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                        0,
                        new[] { new UtxoValue("lovelace", utxoValueLovelace) }),
                    sale);
            };

            action.Should().Throw<MaxAllowedPurchaseQuantityExceededException>();
        }

        public NiftySalePeriod GenerateSalePeriod(
            long costPerTokenLovelace = 10000000, int maxAllowedPurchaseQuantity = 5, bool isActive = true)
        {
            return new NiftySalePeriod(
                Id: Guid.Parse("69da836f-9e0b-4ec4-98e8-094efaeac38b"),
                CollectionId: Guid.Parse("e271ae1a-8831-4afd-8cb7-67a55c2bd6cd"),
                PolicyId: "95c248e17f0fc35be4d2a7d186a84cdcda5b99d7ad2799ebe98a9865",
                Name: "Preview Launch #1",
                Description: "Limited 500 item launch",
                LovelacesPerToken: costPerTokenLovelace,
                SaleAddress: "addr_test1vre6wmde3qz7h7eerk98lgtkuzjd5nfqj4wy0fwntymr20qee2cxk",
                IsActive: isActive,
                From: new DateTime(2022, 11, 30, 0, 0, 0, DateTimeKind.Utc),
                To: new DateTime(2022, 12, 30, 0, 0, 0, DateTimeKind.Utc),
                TotalReleaseQuantity: 500,
                MaxAllowedPurchaseQuantity: maxAllowedPurchaseQuantity);
        }
    }
}
