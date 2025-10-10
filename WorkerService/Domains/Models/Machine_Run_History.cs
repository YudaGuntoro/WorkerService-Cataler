using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class Machine_Run_History
    {
        public int Id { get; set; }
        public int LineNo { get; set; }  // pakai uint karena kolom `int unsigned`
        public string? LineName { get; set; }
        public int Status { get; set; } 
        public DateTime DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
    }
}
