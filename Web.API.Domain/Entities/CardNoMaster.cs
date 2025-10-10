using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class CardNoMaster
{
    public int Id { get; set; }

    public string? LineNo { get; set; }

    public string? LineName { get; set; }

    public string? CardNo { get; set; }

    public string? ProductName { get; set; }

    public string? MaterialName { get; set; }

    public string? PartNo { get; set; }

    public string? MaterialNo { get; set; }

    public string? SubstrateName { get; set; }

    public string? TactTime { get; set; }

    public string? PassHour { get; set; }

    public string? CoatWidthMin { get; set; }

    public string? CoatWidthTarget { get; set; }

    public string? CoatWidthMax { get; set; }

    public string? SolidityMin { get; set; }

    public string? SolidityTarget { get; set; }

    public string? SolidityMax { get; set; }

    public string? Viscosity100Min { get; set; }

    public string? Viscosity100Max { get; set; }

    public string? Viscosity1Min { get; set; }

    public string? Viscosity1Max { get; set; }

    public string? PHmin { get; set; }

    public string? PHmax { get; set; }

    public sbyte? IsDeleted { get; set; }
}
