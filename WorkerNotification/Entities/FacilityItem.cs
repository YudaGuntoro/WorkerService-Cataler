using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerNotification.Entities
{
    public class FacilityItem
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("lineMasterId")] public int LineMasterId { get; set; }
        [JsonProperty("lineName")] public string? LineName { get; set; }
        [JsonProperty("category")] public string? Category { get; set; }
        [JsonProperty("device")] public string? Device { get; set; }
        [JsonProperty("result")] public decimal Result { get; set; }
        [JsonProperty("limitValue")] public decimal LimitValue { get; set; }
        [JsonProperty("collectDate")] public DateTime CollectDate { get; set; }
        [JsonProperty("status")] public string? Status { get; set; }
    }
}
