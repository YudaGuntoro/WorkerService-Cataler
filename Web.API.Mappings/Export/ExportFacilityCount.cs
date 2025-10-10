using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Export
{
    public class ExportFacilityCount
    {
       
        public int LineName { get; set; }

        public string Category { get; set; } = null!;

        public string Device { get; set; } = null!;

        public long? Result { get; set; }

        public long LimitValue { get; set; }

        public DateTime CollectDate { get; set; }

        public string Status { get; set; } = null!;
       
    }
}
