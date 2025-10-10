using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ProductionPlanMaster
{
    public int Id { get; set; }

    public int LineMasterId { get; set; }

    public int ProductMasterId { get; set; }

    public DateOnly PlanDate { get; set; }

    public int PlanQty { get; set; }

    public int WorkStatusMasterId { get; set; }

    public string? FileName { get; set; }

    public DateOnly? DateFile { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ProductionHistory? ProductionHistory { get; set; }
}
