using Microsoft.AspNetCore.Http;
using MiniExcelLibs.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class UploadExcel
    {
        public IFormFile File { get; set; }       // File upload
    }
}
