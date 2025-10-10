using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class CZEC2_Machine_Data_Details
    {
        [JsonProperty("M/C_Status")]
        public decimal MCStatus { get; set; }

        [JsonProperty("Line_No")]
        public decimal LineNo { get; set; }

        [JsonProperty("Card_No")]
        public decimal CardNo { get; set; }

        [JsonProperty("Plan_Day")]
        public decimal Plan { get; set; }

        [JsonProperty("Target_Day")]
        public decimal TargetDay { get; set; }

        [JsonProperty("Actual_Day")]
        public decimal Actual { get; set; }

        [JsonProperty("OK_Count")]
        public decimal OKCount { get; set; }

        [JsonProperty("NG_Count")]
        public decimal NGCount { get; set; }

        [JsonProperty("COAT_Target")]
        public decimal COATTarget { get; set; }

        [JsonProperty("COAT_Result")]
        public decimal COATResult { get; set; }

        [JsonProperty("A_COAT_1st")]
        public decimal ACOAT1st { get; set; }

        [JsonProperty("A_COAT_2nd")]
        public decimal ACOAT2nd { get; set; }

        [JsonProperty("A_COAT_3rd")]
        public decimal ACOAT3rd { get; set; }

        [JsonProperty("B_COAT_1st")]
        public decimal BCOAT1st { get; set; }

        [JsonProperty("B_COAT_2nd")]
        public decimal BCOAT2nd { get; set; }

        [JsonProperty("B_COAT_3rd")]
        public decimal BCOAT3rd { get; set; }
    }

}
