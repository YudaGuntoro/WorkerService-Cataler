using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.ProductionCount
{
    public class ProductionCountMasterDto
    {
        public int Id { get; set; }
        public int? LineMasterId { get; set; }
        public int? LineNo { get; set; }
        public string? LineName { get; set; }
        public string DispOrder { get; set; } = string.Empty; // char(2)
        public TimeOnly DataDate { get; set; }                 // TIME
        public int OperationHour { get; set; }                // tinyint
        public int? PathControl { get; set; }
        public int? Target { get; set; }
    }
}
