using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class ProductionPlanUploadExcel
    {
        public string LineName { get; set; }
        public string ProductName { get; set; }
        public string PlanDate { get; set; }
        public string PlanQty { get; set; }
    }
}
