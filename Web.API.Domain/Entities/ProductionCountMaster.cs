using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ProductionCountMaster
{
    public int Id { get; set; }

    public int? LineMasterId { get; set; }

    public string DispOrder { get; set; } = null!;

    public TimeOnly DataDate { get; set; }

    public int OperationHour { get; set; }

    public int? PathControl { get; set; }

    public int? Target { get; set; }
}
