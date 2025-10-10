using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Models
{
    public class Shift_Inside_Gap_Result
    {
        public string Mode { get; init; } = default!;                // "INSIDE" atau "GAP"
        public string? CurrentCode { get; init; }                    // jika INSIDE
        public string? PrevCode { get; init; }                       // jika GAP atau INSIDE (prev ada)
        public DateTime? PrevEndDateTime { get; init; }              // waktu akhir shift sebelumnya (jika ada)
        public string? NextCode { get; init; }                       // jika GAP atau INSIDE (next ada)
        public DateTime? NextStartDateTime { get; init; }            // waktu mulai shift berikutnya (jika ada)
        public int? GapSeconds { get; init; }                        // hanya GAP
        public int? GapMinutes { get; init; }                        // hanya GAP
        public DateTime? StartDateTime { get; init; }                // hanya INSIDE
        public DateTime? EndDateTime { get; init; }                  // hanya INSIDE
        public int? SecondsToShiftEnd { get; init; }                 // hanya INSIDE
        public int? MinutesToShiftEnd { get; init; }                 // hanya INSIDE
    }
}
