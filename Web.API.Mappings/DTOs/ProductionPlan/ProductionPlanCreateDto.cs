using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.ProductionPlan
{
    public class ProductionPlanCreateDto
    {
        public int LineMasterId { get; set; }
        public int ProductMasterId { get; set; }
        public DateOnly PlanDate { get; set; }
        public int PlanQty { get; set; }
    }
}
