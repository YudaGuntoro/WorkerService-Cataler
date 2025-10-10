using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class ProductMaster
{
    public int Id { get; set; }

    public int LineNo { get; set; }

    public string ProductName { get; set; } = null!;

    public sbyte? IsDeleted { get; set; }
}
