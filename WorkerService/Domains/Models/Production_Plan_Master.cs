using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class Production_Plan_Master
    {
        public int Id { get; set; }
        public string? ProducName { get; set; }
        public DateTime? PlanDate { get; set; }
        public decimal? PlanQty { get; set; }
        public string? ShiftCode { get; set; }
    }
}
