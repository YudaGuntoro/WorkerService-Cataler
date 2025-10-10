using System;
using Microsoft.Extensions.Logging;

namespace WorkerService.Helper
{
    internal static class SignalHelper
    {
        public static void HandleSignal(
            ref bool? prev,
            ref bool waitingForRelease,
            bool current,
            ref int counter,
            string type,
            string machine,
            ILogger logger)
        {
            if (!waitingForRelease)
            {
                if (prev == false && current == true)
                {
                    waitingForRelease = true; // tunggu jatuh ke false
                }
            }
            else
            {
                if (current == false)
                {
                    counter++;
                    waitingForRelease = false;
                    logger.LogInformation($"[Counting] {type} {machine} Naik: {counter}");
                }
            }

            prev = current;
        }
    }
}
