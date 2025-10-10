using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class ProductionCountMasterUpdateRequest
    {
        public int? LineMasterId { get; set; }
        public string DispOrder { get; set; } = string.Empty;
        public TimeOnly DataDate { get; set; }
        public sbyte OperationHour { get; set; }
        public int? PathControl { get; set; }
        public int? Target { get; set; }
    }
}
