using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class ProductionPlanUploadExcel
    {
        public string LINE_NO { get; set; }
        public string PRODUCT_NAME { get; set; }
        public string QUANTITY_TARGET { get; set; }
        public string DATE { get; set; }
    }
}
