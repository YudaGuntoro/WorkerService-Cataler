using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerNotification.Helper
{
    public class ApiOptions
    {
        public string? BaseUrl { get; set; }
        public int EarlyRefreshSeconds { get; set; } = 0;
        public MachineEntry[] Machines { get; set; } = Array.Empty<MachineEntry>();
    }

    public class MachineEntry
    {
        public string? Name { get; set; }
        public string? Url { get; set; }   // bisa relative (/api/...) atau absolute (http://...)
    }
}
