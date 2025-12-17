using System;
using System.ComponentModel.DataAnnotations;

namespace Web.API.Mappings.Request
{
    public class CoatWidthControlCreate
    {
        [Required]
        public int LineMasterId { get; set; }

        [Required]
        public string SubProductName { get; set; }

        public int? CoatingNo { get; set; }

        public DateOnly? RecordDate { get; set; }

        public double? KpaRecommend { get; set; }

        public decimal? Solidity { get; set; }

        public int? Vis100rpm { get; set; }
        public int? Vis1rpm { get; set; }
        public int? Bcd4digit { get; set; }

        public double? CoatingPressureKpa { get; set; }

        public double? CoatWidthAvg { get; set; }

        public int? ProdMemberId { get; set; }
        public int? ProdStaffId { get; set; }

        public int? Emisi { get; set; }

        [MaxLength(50)]
        public string? Remark { get; set; }

        public double? KpaAccuracy { get; set; }
    }
}
