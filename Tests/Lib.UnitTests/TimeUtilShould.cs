using FluentAssertions;
using System;
using System.Globalization;
using Xunit;

namespace Mintsafe.Lib.UnitTests;

public class TimeUtilShould
{
    [Theory]
    [InlineData(2021, 10, 28, 14, 0, 4, 41060390)]
    [InlineData(2022, 1, 28, 19, 0, 0, 49027186)]
    [InlineData(2023, 7, 23, 0, 0, 0, 95701186)]
    public void Return_Correct_Testnet_Slot_Utc_DateTime(
        int year, int month, int day, int hour, int minute, int second,
        int expectedSlot)
    {
        var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

        var slot = TimeUtil.GetTestnetSlotAt(dateTime);

        slot.Should().Be(expectedSlot);
    }

    [Theory]
    [InlineData(41060390, 2021, 10, 28, 14, 0, 4)]
    [InlineData(49027186, 2022, 1, 28, 19, 0, 0)]
    [InlineData(74686216, 2022, 11, 21, 18, 30, 30)]
    public void Return_Correct_Utc_Time_From_Testnet_Slot(
        int slot, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        var expectedDateTimeUtc = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond, DateTimeKind.Utc);

        var dateTimeUtc = TimeUtil.GetUtcTimeFromTestnetSlot(slot);

        dateTimeUtc.Should().Be(expectedDateTimeUtc);
    }

    [Theory]
    [InlineData(2021, 10, 28, 14, 1, 0, 43863369)]
    [InlineData(2022, 4, 1, 0, 0, 0, 57204909)]
    [InlineData(2023, 7, 23, 0, 0, 0, 98504109)]
    public void Return_Correct_Mainnet_Slot_For_Utc_DateTime(
        int year, int month, int day, int hour, int minute, int second,
        int expectedSlot)
    {
        var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

        var slot = TimeUtil.GetMainnetSlotAt(dateTime);

        slot.Should().Be(expectedSlot);
    }

    [Theory]
    [InlineData(43863369, 2021, 10, 28, 14, 1, 0)]
    [InlineData(51830109, 2022, 1, 28, 19, 0, 0)]
    [InlineData(98504109, 2023, 7, 23, 0, 0, 0)]
    public void Return_Correct_Utc_Time_From_Mainnet_Slot(
        int slot, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        var expectedDateTimeUtc = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond, DateTimeKind.Utc);

        var dateTimeUtc = TimeUtil.GetUtcTimeFromMainnetSlot(slot);

        dateTimeUtc.Should().Be(expectedDateTimeUtc);
    }

    [Theory]
    [InlineData("2021-10-26T19:30:00Z", 40907386)]
    [InlineData("2021-12-26T00:00:00Z", 46107586)]
    [InlineData("2022-11-15T00:00:00Z", 74101186)]
    public void Return_Correct_Testnet_Slot_Based_On_Iso8601_Dates(string datetimeIso8601, int expectedSlot)
    {
        DateTime.TryParseExact(
            datetimeIso8601,
            @"yyyy-MM-dd\THH:mm:ss\Z",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var dateTimeParsed);
        var utcDateTime = dateTimeParsed.ToUniversalTime();

        var slot = TimeUtil.GetTestnetSlotAt(utcDateTime);

        slot.Should().Be(expectedSlot);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Throws_Argument_Exception_For_DateTimeKind_Not_Utc(DateTimeKind unsupportedKind)
    {
        var dateTime = new DateTime(2021, 1, 1, 0, 0, 0, unsupportedKind);

        Action action = () =>
        {
            var slot = TimeUtil.GetTestnetSlotAt(dateTime);
        };

        action.Should().Throw<ArgumentException>();
    }
}
