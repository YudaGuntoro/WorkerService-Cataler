using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class Timestamp_Captured_Dto
    {
        public DateTime GatewayTimestamp { get; set; }
        public DateTime SystemTimestamp { get; set; }
    }
}
