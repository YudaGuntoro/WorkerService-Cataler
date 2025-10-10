using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ProductionHistory
{
    public int Id { get; set; }

    public int ProductionPlanId { get; set; }

    public uint ActualQty { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual ProductionPlanMaster ProductionPlan { get; set; } = null!;
}
