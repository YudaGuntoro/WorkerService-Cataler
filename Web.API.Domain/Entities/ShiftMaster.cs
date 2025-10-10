using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ShiftMaster
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<ShiftSchedule> ShiftSchedules { get; set; } = new List<ShiftSchedule>();
}
