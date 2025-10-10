using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class AlarmMaster
{
    public int Id { get; set; }

    public int StatusCode { get; set; }

    public string FailureDetails { get; set; } = null!;
}
