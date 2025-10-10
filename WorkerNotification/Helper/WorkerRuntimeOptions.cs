using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerNotification.Helper
{
    public class WorkerRuntimeOptions
    {
        public int PollSeconds { get; set; } = 5;
        public int WarningTtlHours { get; set; } = 24;
        public int MaintenanceTtlHours { get; set; } = 2;
    }
}
