using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class AlarmLogHistory
{
    public int Id { get; set; }

    public string Message { get; set; } = null!;

    public int LineNo { get; set; }

    public DateTime Timestamp { get; set; }
}
