using System;
using System.Collections.Generic;

namespace Web.API.Models;

public partial class ProductionPlanMaster
{
    public int Id { get; set; }
    public string? LineName { get; set; }
    public string? ProductName { get; set; }
    public DateOnly? PlanDate { get; set; }
    public int? PlanQty { get; set; }
    public string? WorkStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
}