using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerLogger.Domains.Models
{
    public class Alarm_Log_History
    {
        public int Id { get; set; }

        public string Message { get; set; } = default!;

        public int LineNo { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
