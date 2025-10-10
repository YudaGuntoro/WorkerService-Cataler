using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.HistoryList
{
    public class GetAlarmLogDto
    {
        public int LineNo { get; set; }
        public string LineName { get; set; }
        public string Message { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
