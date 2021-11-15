using System;

namespace Mintsafe.Lib;

public static class TimeUtil
{
    public static int GetTestnetSlotAt(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{nameof(utcDateTime)} must be DateTimeKind.Utc");

        var then = new DateTime(2021, 10, 28, 14, 0, 4, DateTimeKind.Utc);
        var slotThen = 41060390;

        // Assumption is that 1 slot = 1 second although this can be adjusted through protocol updates
        var secondsDiff = (utcDateTime - then).TotalSeconds;

        return slotThen + (int)secondsDiff;
    }

    public static DateTime GetUtcTimeFromTestnetSlot(long slot)
    {
        var then = new DateTime(2021, 10, 28, 14, 0, 4, DateTimeKind.Utc);
        var slotThen = 41060390;

        var slotDifference = slot - slotThen;

        var time = then.AddSeconds(slotDifference);

        return time;
    }

    public static int GetMainnetSlotAt(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{nameof(utcDateTime)} must be DateTimeKind.Utc");

        var then = new DateTime(2021, 10, 28, 14, 1, 0, DateTimeKind.Utc);
        var slotThen = 43863369;

        // Assumption is that 1 slot = 1 second although this can be adjusted through protocol updates
        var secondsDiff = (utcDateTime - then).TotalSeconds;

        return slotThen + (int)secondsDiff;
    }

    public static DateTime GetUtcTimeFromMainnetSlot(long slot)
    {
        var then = new DateTime(2021, 10, 28, 14, 1, 0, DateTimeKind.Utc);
        var slotThen = 43863369;

        var slotDifference = slot - slotThen;

        var time = then.AddSeconds(slotDifference);

        return time;
    }
}
