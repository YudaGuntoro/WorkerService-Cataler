using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class CZEC1_Machine_Data
    {
        [JsonProperty("DATA")]
        public CZEC1_Machine_Data_Details CZEC1_Data { get; set; }
        [JsonProperty("TOOL")]
        public CZEC1_Machine_Tool_Details CZEC1_Tool { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
