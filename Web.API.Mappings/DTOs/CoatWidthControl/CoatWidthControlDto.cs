using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.CoatWidthControl
{
    public sealed class CoatWidthControlDto
    {
        public int Id { get; set; }
        public int LineMasterId { get; set; }
        public string? SubProductName { get; set; }
        public int CoatingNo { get; set; }
        public DateOnly RecordDate { get; set; }
        public decimal? KpaRecommend { get; set; }
        public decimal? Solidity { get; set; }
        public int? Vis100rpm { get; set; }
        public int? Vis1rpm { get; set; }
        public int? Bcd4digit { get; set; }
        public decimal? CoatingPressureKpa { get; set; }
        public decimal? CoatWidthAvg { get; set; }
        public int ProdMemberId { get; set; }
        public int ProdStaffId { get; set; }
        public string? ProdMemberName { get; set; }
        public string? ProdStaffName { get; set; }
        public int? Emisi { get; set; }
        public string? Remark { get; set; }
        public decimal? KpaAccuracy { get; set; }
        public DateTime CreatedAt { get; set; }

        // enrich
        public string LineName { get; set; } = "Unknown";
        public string ProductName { get; set; } = "Unknown";
    }
}
