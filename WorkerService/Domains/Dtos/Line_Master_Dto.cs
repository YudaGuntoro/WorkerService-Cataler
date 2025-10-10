using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class Line_Master_Dto
    {
        public int Id { get; set; }
        public int LineNo { get; set; }
        public string LineName { get; set; } = null!;
    }
}
