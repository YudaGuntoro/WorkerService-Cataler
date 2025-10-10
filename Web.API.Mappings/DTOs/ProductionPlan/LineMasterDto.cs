using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.ProductionPlan
{
    public class LineMasterDto
    {
        public int Id { get; set; }
        public int LineNo { get; set; }
        public string LineName { get; set; } = null!;
    }
}
