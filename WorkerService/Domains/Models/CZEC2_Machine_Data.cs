using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class CZEC2_Machine_Data
    {
        [JsonProperty("DATA")]
        public CZEC2_Machine_Data_Details CZEC2_Data { get; set; }

        [JsonProperty("TOOL")]
        public CZEC2_Machine_Tool_Details CZEC2_Tool { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
