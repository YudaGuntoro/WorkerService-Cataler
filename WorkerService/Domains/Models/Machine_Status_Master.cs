using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class Machine_Status_Master
    {
        public int Id { get; set; }
        public byte StatusCode { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}
