using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class FacilityCount
{
    public int Id { get; set; }

    public int LineMasterId { get; set; }

    public string Category { get; set; } = null!;

    public string Device { get; set; } = null!;

    public long? Result { get; set; }

    public long LimitValue { get; set; }

    public DateTime CollectDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual LineMaster LineMaster { get; set; } = null!;
}
