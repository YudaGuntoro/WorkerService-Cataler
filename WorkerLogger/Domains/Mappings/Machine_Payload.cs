using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerLogger.Domains.Mappings
{
    public class MachineDataModel
    {
        public ProductionDetails? ProductionDetails { get; set; }   // ← bisa null
        public MachineStatus? MachineStatus { get; set; }           // ← bisa null
        public CycleTime? CycleTime { get; set; }                   // ← bisa null
        public MachineRuntime? MachineRuntime { get; set; }         // ← bisa null
        public TimeStamp? TimeStamp { get; set; }                   // ← bisa null
    }

    public class ProductionDetails
    {
        public decimal? LineNo { get; set; }
        public string? LineName { get; set; }
        public decimal? CardNo { get; set; }
        public string? ProductName { get; set; }
        public string? MaterialName { get; set; }
        public decimal? PartNo { get; set; }
        public decimal? MaterialNo { get; set; }
        public string? SubstrateName { get; set; }
        public decimal? TactTime { get; set; }
        public decimal? PassHour { get; set; }
        public decimal? CoatWidthMin { get; set; }
        public decimal? CoatWidthTarget { get; set; }
        public decimal? CoatWidthMax { get; set; }
        public decimal? SolidityMin { get; set; }
        public decimal? SolidityTarget { get; set; }
        public decimal? SolidityMax { get; set; }
        public decimal? Viscosity100Min { get; set; }
        public decimal? Viscosity100Max { get; set; }
        public decimal? Viscosity1Min { get; set; }
        public decimal? Viscosity1Max { get; set; }
        public decimal? PHMin { get; set; }
        public decimal? PHMax { get; set; }
        public decimal? ActualProduction { get; set; }
        public decimal? MachinePlan { get; set; }
        public decimal? SystemPlan { get; set; }
    }

    public class MachineStatus
    {
        public string? StatusCode { get; set; }
        public decimal? PCSH { get; set; }
        public decimal? PCSD { get; set; }
        public decimal? PCSHTarget { get; set; }   // ← di payload bisa null
        public decimal? PCSDTarget { get; set; }
        public decimal? OATarget { get; set; }     // ← ada di payload contoh
        public decimal? NGProduct { get; set; }
        public decimal? OKProduct { get; set; }
        public decimal? Progress { get; set; }
        public decimal? COCount { get; set; }
        public decimal? OA { get; set; }           // ← ada di payload contoh
    }

    public class CycleTime
    {
        public decimal? Target { get; set; }
        public decimal? Result { get; set; }
        public string? Judgement { get; set; }
        public decimal? A_COAT_1 { get; set; }
        public decimal? A_COAT_2 { get; set; }
        public decimal? A_COAT_3 { get; set; }
        public decimal? B_COAT_1 { get; set; }
        public decimal? B_COAT_2 { get; set; }
        public decimal? B_COAT_3 { get; set; }
    }

    public class MachineRuntime
    {
        public string? OFF { get; set; }
        public string? MALFUNCTION { get; set; }
        public string? RUNNING { get; set; }
        public string? SCHDT { get; set; }
        public string? CO { get; set; }            // ← ada di payload contoh
        public decimal? OFF_MIN { get; set; }
        public decimal? MALFUNCTION_MIN { get; set; }
        public decimal? RUNNING_MIN { get; set; }
        public decimal? SCHDT_MIN { get; set; }
        public decimal? CO_MIN { get; set; }
    }

    public class TimeStamp
    {
        public DateTime GatewayTimestamp { get; set; }
        public DateTime SystemTimestamp { get; set; }
    }
}
