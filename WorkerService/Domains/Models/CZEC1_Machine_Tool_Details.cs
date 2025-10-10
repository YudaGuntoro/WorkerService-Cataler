using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class CZEC1_Machine_Tool_Details
    {
        [JsonPropertyName("AD21A")]
        public decimal AD21A { get; set; }

        [JsonPropertyName("AD21B")]
        public decimal AD21B { get; set; }

        [JsonPropertyName("CY21A")]
        public decimal CY21A { get; set; }

        [JsonPropertyName("CY21B")]
        public decimal CY21B { get; set; }

        [JsonPropertyName("M11X")]
        public decimal M11X { get; set; }

        [JsonPropertyName("M11Y")]
        public decimal M11Y { get; set; }

        [JsonPropertyName("M21AX")]
        public decimal M21AX { get; set; }

        [JsonPropertyName("M21AY")]
        public decimal M21AY { get; set; }

        [JsonPropertyName("M21BX")]
        public decimal M21BX { get; set; }

        [JsonPropertyName("M21BY")]
        public decimal M21BY { get; set; }
    }
}
