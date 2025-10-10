using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.MasterData
{
    public class GetSubProductMasterListDto
    {
        public int Id { get; set; } 
        public int LineNo { get; set; } 
        public int CardNo { get; set; }
        public string ProductName{ get; set; } = null!;
    }

    public class GetSpesificMasterByCardNoDto
    {
        public int LineNo { get; set; }
        public int CardNo { get; set; }
    }
}
