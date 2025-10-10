using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class Cycle_Time_Dto
    {
        public decimal? Target { get; set; }
        public decimal? Result { get; set; }
        public string Judgement { get; set; }
        public float? A_COAT_1{ get; set; }
        public float? A_COAT_2 { get; set; }
        public float? A_COAT_3 { get; set; }
        public float? B_COAT_1 { get; set; }
        public float? B_COAT_2 { get; set; }
        public float? B_COAT_3 { get; set; }
    }
}
