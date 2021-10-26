using System;

namespace NiftyLaunchpad.Lib
{
    public static class TimeUtil
    {
        public static int GetSlotAt(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"{nameof(utcDateTime)} must be DateTimeKind.Utc");
            
            var then = new DateTime(2021, 10, 26, 19, 15, 0, DateTimeKind.Utc);
            var slotThen = 43709407;

            // Assumption is that 1 slot = 1 second although this can be adjusted through protocol updates
            var secondsDiff = (utcDateTime - then).TotalSeconds;

            return slotThen + (int)secondsDiff;
        }
    }
}
