using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class Card_No_Master
    {
        public int Id { get; set; }
        public decimal? LineNo { get; set; }
        public string? LineName { get; set; }
        public  decimal? CardNo { get; set; }
        public string? ProductName { get; set; }
        public string? MaterialName { get; set; }
        public decimal? PartNo { get; set; }
        public decimal? MaterialNo { get; set; }
        public string? SubstrateName { get; set; }
        public decimal? TactTime { get; set; }
        public decimal? PassHour { get; set; }
        public decimal? CoatWidthMin { get; set; }
        public decimal? CoatWidthTarget { get; set; }
        public decimal? CoatWidthMax { get; set; }
        public decimal? SolidityMin { get; set; }
        public decimal? SolidityTarget { get; set; }
        public decimal? SolidityMax { get; set; }
        public decimal? Viscosity100Min { get; set; }
        public decimal? Viscosity100Max { get; set; }
        public decimal? Viscosity1Min { get; set; }
        public decimal? Viscosity1Max { get; set; }
        public decimal? PHMin { get; set; }
        public decimal? PHMax { get; set; }
    }
}
