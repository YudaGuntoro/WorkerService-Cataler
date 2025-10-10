using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public static class Machine2_Counter_State
    {
     

        public static int OK_M2 { get; set; }
        public static int NG_M2 { get; set; }
        public static int ActualProduction{ get; set; }
        public static int PCSDMachine { get; set; }
        public static int PCSHMachine { get; set; }

    }
}
