using System;
using System.Collections.Generic;

namespace Web.API.Models;

public partial class MachineMaster
{
    public int Id { get; set; }
    public int? LineNo { get; set; }
    public string LineName { get; set; } = null!;
}
