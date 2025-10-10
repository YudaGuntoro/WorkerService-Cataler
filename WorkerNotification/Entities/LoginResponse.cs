using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerNotification.Entities
{
    public class LoginResponse
    {
        [JsonProperty("token")] public string Token { get; set; }
        [JsonProperty("expiresAt")] public string? ExpiresAt { get; set; }
        [JsonProperty("userId")] public string? UserId { get; set; }
        [JsonProperty("userName")] public string? UserName { get; set; }
        [JsonProperty("role")] public string? Role { get; set; }
    }
}
