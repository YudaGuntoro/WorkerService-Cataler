using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class CoatWidthControl
{
    public int Id { get; set; }

    public int LineMasterId { get; set; }

    public string? SubProductName { get; set; }

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

    public string? Remark { get; set; }

    public double? KpaAccuracy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual LineMaster LineMaster { get; set; } = null!;
}
