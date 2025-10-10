using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerLogger.Domains.Mappings
{
    public class Alarm_Log_Payload
    {
        public string Status { get; set; } = default!;
        public DateTime TriggerTime { get; set; }
        public DateTime RecoverTime { get; set; }
        public string Message { get; set; } = default!;
        public DateTime Ts { get; set; }
    }
}
