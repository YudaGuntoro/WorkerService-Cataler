using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerLogger.Domains.Models
{
    public class Production_Plan_Master
    {
        public int Id { get; set; }
        public string? ProductName { get; set; }
        public DateTime? PlanDate { get; set; }
        public decimal? PlanQty { get; set; }
    }
}
