using System;
using System.Collections.Generic;

namespace Web.API.Models;

public partial class MachineStatusMaster
{
    public int Id { get; set; }

    public sbyte StatusCode { get; set; }

    public string StatusLabel { get; set; } = null!;

    public string StatusColor { get; set; } = null!;
}
