using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class OaTarget
{
    public int Id { get; set; }

    public int LineNo { get; set; }

    public int Target { get; set; }
}
