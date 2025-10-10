using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ShiftSchedule
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public string ScheduleType { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public bool CrossesMidnight { get; set; }

    public bool? IsActive { get; set; }

    public virtual ShiftMaster Shift { get; set; } = null!;
}
