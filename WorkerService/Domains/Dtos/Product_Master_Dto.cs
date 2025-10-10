using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.Domains.Dtos
{
    public class Product_Master_Dto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = null!;
    }
}
