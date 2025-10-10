using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class Machine_Status_Dto
    {
        //MachineStatus
        public string StatusCode { get; set; }
        public decimal? PCSH { get; set; }
        public decimal? PCSD { get; set; }
        public decimal? PCSHTarget { get; set; }
        public decimal? PCSDTarget { get; set; }
        public decimal? OATarget { get; set; }
        public decimal? NGProduct { get; set; }
        public decimal? OKProduct { get; set; }
        public decimal Progress { get; set; }
        public decimal COCount { get; set;}
        public double OA { get; set; }
    }
}