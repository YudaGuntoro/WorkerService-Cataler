using System;
using System.Collections.Generic;

namespace Web.API.Domain.Entities;

public partial class TelegramToken
{
    public string Id { get; set; } = null!;

    public string Token { get; set; } = null!;
}
