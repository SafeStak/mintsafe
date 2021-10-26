using FluentAssertions;
using System;
using System.Globalization;
using Xunit;

namespace NiftyLaunchpad.Lib.UnitTests
{
    public class TimeUtilShould
    {
        [Theory]
        [InlineData("2021-10-26T19:30:00Z", 43710307)]
        [InlineData("2021-12-26T00:00:00Z", 48910507)]
        [InlineData("2022-11-15T00:00:00Z", 76904107)]
        public void Return_Correct_Slot_In_The_Future(string datetimeIso8601, int expectedSlot)
        {
            DateTime.TryParseExact(
                datetimeIso8601,
                @"yyyy-MM-dd\THH:mm:ss\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, 
                out var dateTimeParsed);
            var utcDateTime = dateTimeParsed.ToUniversalTime();

            var slot = TimeUtil.GetSlotAt(utcDateTime);

            slot.Should().Be(expectedSlot); 
        }

        [Theory]
        [InlineData(2021, 10, 26, 19, 15, 0, 43709407)]
        public void Return_Correct_Slot_In_The_Past(
            int year, int month, int day, int hour, int minute, int second,
            int expectedSlot)
        {
            var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

            var slot = TimeUtil.GetSlotAt(dateTime);

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
                var slot = TimeUtil.GetSlotAt(dateTime);
            };

            action.Should().Throw<ArgumentException>();
        }
    }
}
