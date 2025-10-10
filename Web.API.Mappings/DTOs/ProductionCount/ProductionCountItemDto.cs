using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.ProductionCount
{
    public class ProductionCountItemDto
    {
        public int Id { get; set; }
        public int LineMasterId { get; set; }
        public int CardNo { get; set; }
        public int Target { get; set; }
        public int Actual { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Hasil akhir per line (mirip GetHistoryListCycleTimeDto)
    public class GetHistoryListProductionCountDto
    {
        public int lineNo { get; set; }
        public string lineName { get; set; } = string.Empty;
        public List<ProductionCountItemDto> Details { get; set; } = new();
    }
}
