using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ModelChangeLog
{
    public long Id { get; set; }

    public int LineNo { get; set; }

    public string ModelName { get; set; } = null!;

    public DateTime StartRunTime { get; set; }

    public DateTime CreatedAt { get; set; }
}
