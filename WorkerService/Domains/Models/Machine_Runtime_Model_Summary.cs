using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class Machine_Runtime_Model_Summary
    {
        public int status { get; set; }
        public int totalRuntime { get; set; }              // menit (int) dari kolom 'totalRuntime' SP
    }
}
