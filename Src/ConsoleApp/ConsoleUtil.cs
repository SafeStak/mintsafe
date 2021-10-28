using System;
using System.Threading;

namespace NiftyLaunchpad.ConsoleApp
{
    public static class ConsoleUtil
    {
        public static CancellationTokenSource SetupUserInputCancellationTokenSource()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            return cts;
        }
    }
}
