using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class Breaktime
{
    public int Id { get; set; }

    public TimeOnly BreakTimeParam { get; set; }

    public int BreakMin { get; set; }
}
