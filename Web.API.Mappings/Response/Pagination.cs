using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Response
{
    public class Pagination
    {
        public int Curr_Page { get; set; }
        public int Total_Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
    }
}
