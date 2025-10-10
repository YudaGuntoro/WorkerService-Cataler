using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class Machine_Runtime_Dto
    {
        public TimeSpan OFF { get; set; }
        public TimeSpan MALFUNCTION { get; set; }
        public TimeSpan RUNNING { get; set; }
        public TimeSpan SCHDT { get; set; }
        public TimeSpan CO { get; set; }


        public double OFF_MIN { get; set; }
        public double MALFUNCTION_MIN{ get; set; }
        public double RUN_MIN { get; set; }
        public double SCHDT_MIN { get; set; }
        public double CO_MIN { get; set; }
    }
}
