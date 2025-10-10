using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class ShiftDto
    {
        public string Code { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string ScheduleType { get; init; } = default!;     // 'NORMAL' atau 'RAMADAN'
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public bool CrossesMidnight { get; init; }
    }
}
