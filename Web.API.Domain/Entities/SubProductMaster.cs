using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class SubProductMaster
{
    public int Id { get; set; }

    public int ProductMasterId { get; set; }

    public string SubProductName { get; set; } = null!;

    public virtual ProductMaster ProductMaster { get; set; } = null!;
}
