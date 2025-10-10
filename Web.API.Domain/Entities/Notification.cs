using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class Notification
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Target { get; set; } = null!;

    public int Type { get; set; }

    public bool Problems { get; set; }

    public bool ChangeOver { get; set; }

    public bool FacilityCount { get; set; }
}
