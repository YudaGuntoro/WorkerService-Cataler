using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class UpdateFacilityCount
    {
        public string? Device { get; set; }
        public string? Category { get; set; }
        public long? LimitValue { get; set; }
    }
}
