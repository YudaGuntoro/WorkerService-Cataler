using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.Gateway
{
    public class ToolPayload
    {
        [JsonProperty("TOOL")]
        public Dictionary<string, long?> TOOL { get; set; } = new();

        [JsonProperty("timestamp")]
        public DateTime? Timestamp { get; set; }
    }
}
