using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class CalculateKpaRecommendationRequest
    {
        [Required]
        public int LineNo { get; set; }

        [Required]
        public string SubProductName { get; set; }

        public decimal? Solidity { get; set; }
        public int? Vis100rpm { get; set; }
        public int? Vis1rpm { get; set; }
        public double? CoatingPressureKpa { get; set; }
        public double? CoatWidthAvg { get; set; }
        public double? Emisi { get; set; }
    }
}
