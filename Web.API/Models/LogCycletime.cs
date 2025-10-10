using System;
using System.Collections.Generic;

namespace Web.API.Models;

public partial class LogCycletime
{
    public long Id { get; set; }
    public int MachineId { get; set; }
    public decimal? Target { get; set; }
    public decimal? Result { get; set; }
    public string? Judgement { get; set; }
    public float? ACoat1 { get; set; }
    public float? ACoat2 { get; set; }
    public float? ACoat3 { get; set; }
    public float? BCoat1 { get; set; }
    public float? BCoat2 { get; set; }
    public float? BCoat3 { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? GatewayTimestamp { get; set; }
}
