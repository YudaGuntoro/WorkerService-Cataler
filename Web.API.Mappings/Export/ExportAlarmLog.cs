using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Export
{
    public class ExportAlarmLog
    {
        public int Id { get; set; }
        public int LineNo { get; set; }
        public string LineName { get; set; } = "Unknown";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
