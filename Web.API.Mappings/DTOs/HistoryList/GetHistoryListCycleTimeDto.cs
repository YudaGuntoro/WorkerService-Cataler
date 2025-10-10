using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.API.Domain.Entities;


namespace Web.API.Mappings.DTOs.HistoryList
{
    public class GetHistoryListCycleTimeDto
    {
        public int lineNo { get; set; }
        public string lineName { get; set; }
        public List<LogCycletime> Details { get; set; }
    }
}
