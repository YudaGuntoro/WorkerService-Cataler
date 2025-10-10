using System;
using System.Collections.Generic;

namespace Web.API.Models;

public partial class WorkStatusMaster
{
    public int Id { get; set; }

    public int? StatusCode { get; set; }

    public string? StatusLabel { get; set; }
}
