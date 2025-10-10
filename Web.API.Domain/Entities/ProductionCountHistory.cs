using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ProductionCountHistory
{
    public int Id { get; set; }

    public int LineMasterId { get; set; }

    public int CardNo { get; set; }

    public int Target { get; set; }

    public int Actual { get; set; }

    public DateTime Timestamp { get; set; }
}
