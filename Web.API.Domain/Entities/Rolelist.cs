using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class Rolelist
{
    public int Id { get; set; }

    public string Role { get; set; } = null!;
}
