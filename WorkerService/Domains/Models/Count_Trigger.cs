using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    internal class Count_Trigger
    {
        public TriggerGroup OK_Trigger { get; set; }
        public TriggerGroup NG_Trigger { get; set; }
        public DateTime timestamp { get; set; }

    }
    public class TriggerGroup
    {
        [JsonProperty("Machine 1")]
        public bool Machine_1 { get; set; }

        [JsonProperty("Machine 2")]
        public bool Machine_2 { get; set; }
    }
}
