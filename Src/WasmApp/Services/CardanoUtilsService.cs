using Mintsafe.Lib;

namespace Mintsafe.WasmApp.Services
{
    public class CardanoUtilsService
    {
        private readonly MintsafeAppSettings _settings;

        public CardanoUtilsService(MintsafeAppSettings settings)
        {
            _settings = settings;
        }

        public static long GetMainnetSlotAtUtc(DateTime dateTimeUtc)
        {
            return TimeUtil.GetMainnetSlotAt(dateTimeUtc);
        }

        public static long GetTestnetSlotAtUtc(DateTime dateTimeUtc)
        {
            return TimeUtil.GetTestnetSlotAt(dateTimeUtc);
        }

        public static DateTime GetUtcTimeFromMainnetSlot(long mainnetSlot)
        {
            return TimeUtil.GetUtcTimeFromMainnetSlot(mainnetSlot);
        }

        public static DateTime GetUtcTimeFromTestnetSlot(long testnetSlot)
        {
            return TimeUtil.GetUtcTimeFromTestnetSlot(testnetSlot);
        }
    }
}
