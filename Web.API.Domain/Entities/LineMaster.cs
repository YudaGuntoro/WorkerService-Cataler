using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class LineMaster
{
    public int Id { get; set; }

    public int LineNo { get; set; }

    public string LineName { get; set; } = null!;

    public virtual ICollection<CoatWidthControl> CoatWidthControls { get; set; } = new List<CoatWidthControl>();

    public virtual ICollection<FacilityCount> FacilityCounts { get; set; } = new List<FacilityCount>();
}
