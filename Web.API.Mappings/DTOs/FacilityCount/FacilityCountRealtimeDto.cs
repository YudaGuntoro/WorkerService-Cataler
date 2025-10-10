using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.FacilityCount
{
    public class FacilityCountRealtimeDto
    {
        public int Id { get; set; }
        public int LineMasterId { get; set; }
        public string LineName { get; set; } = default!;
        public string Category { get; set; } = default!;
        public string Device { get; set; } = default!;
        public long? Result { get; set; }
        public long LimitValue { get; set; }
        public DateTime CollectDate { get; set; }
        public string Status { get; set; } = default!;
    }
}