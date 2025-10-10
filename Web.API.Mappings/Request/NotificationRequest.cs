using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class CreateNotificationRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Target { get; set; } = null!;
        public int Type { get; set; }
        public bool Problems { get; set; }
        public bool ChangeOver { get; set; }
        public bool FacilityCount { get; set; }
    }

    public class UpdateNotificationRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Target { get; set; } = null!;
        public int Type { get; set; }
        public bool Problems { get; set; }
        public bool ChangeOver { get; set; }
        public bool FacilityCount { get; set; }
    }
}
