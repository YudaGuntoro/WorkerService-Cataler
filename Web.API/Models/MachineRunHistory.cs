using System;
using System.Collections.Generic;

namespace Web.API.Models;

public partial class MachineRunHistory
{
    public long Id { get; set; }

    public uint LineNo { get; set; }

    public string Status { get; set; } = null!;

    public DateTime DateStart { get; set; }

    public DateTime? DateEnd { get; set; }
}
