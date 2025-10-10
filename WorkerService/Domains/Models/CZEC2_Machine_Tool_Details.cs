using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class CZEC2_Machine_Tool_Details
    {
        // TOOL
        [JsonProperty("M11")]
        public decimal M11 { get; set; }

        [JsonProperty("M21_YA")]
        public decimal M21YA { get; set; }

        [JsonProperty("MY21_YB")]
        public decimal MY21YB { get; set; }

        [JsonProperty("AD21_A")]
        public decimal AD21A { get; set; }

        [JsonProperty("AD21_B")]
        public decimal AD21B { get; set; }

        [JsonProperty("AD22_A")]
        public decimal AD22A { get; set; }

        [JsonProperty("AD22_B")]
        public decimal AD22B { get; set; }

        [JsonProperty("One_M22_A")]
        public decimal OneM22A { get; set; }

        [JsonProperty("One_M22_B")]
        public decimal OneM22B { get; set; }

        [JsonProperty("Two_M22_A")]
        public decimal TwoM22A { get; set; }

        [JsonProperty("One_CY25_A")]
        public decimal OneCY25A { get; set; }

        [JsonProperty("One_CY25_B")]
        public decimal OneCY25B { get; set; }

        [JsonProperty("One_CY26_A")]
        public decimal OneCY26A { get; set; }

        [JsonProperty("One_CY26_B")]
        public decimal OneCY26B { get; set; }

        [JsonProperty("Two_CY25_A")]
        public decimal TwoCY25A { get; set; }

        [JsonProperty("Two_CY25_B")]
        public decimal TwoCY25B { get; set; }

        [JsonProperty("Two_CY26_A")]
        public decimal TwoCY26A { get; set; }

        [JsonProperty("Two_CY26_B")]
        public decimal TwoCY26B { get; set; }
    }

}
