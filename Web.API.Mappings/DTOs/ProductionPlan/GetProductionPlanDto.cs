using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.ProductionPlan
{
    public class GetProductionPlanDto
    {
        public int Id { get; set; }
        public string LineName { get; set; }
        public string ProductName { get; set; }
        public DateOnly? PlanDate { get; set; }
        public int? PlanQty { get; set; }
        public long ActualQty { get; set; }   
        public string? statusLabel { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
