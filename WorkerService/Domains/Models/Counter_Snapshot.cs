using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public sealed class Counter_Snapshot
    {
        public int OK_M1 { get; set; }
        public int NG_M1 { get; set; }
        public int Actual_M1 { get; set; }
        public int PCSH_M1 { get; set; }
        public int PCSD_M1 { get; set; }

        public int OK_M2 { get; set; }
        public int NG_M2 { get; set; }
        public int Actual_M2 { get; set; }
        public int PCSH_M2 { get; set; }
        public int PCSD_M2 { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
