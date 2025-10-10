using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Target { get; set; } = "";
        public int Type { get; set; }
        public bool Problems { get; set; }
        public bool ChangeOver { get; set; }
        public bool FacilityCount { get; set; }
    }
    public class CreateNotificationDto
    {
        public string? Name { get; set; }
        public string Target { get; set; } = "";
        public int Type { get; set; }
        public bool Problems { get; set; }
        public bool ChangeOver { get; set; }
        public bool FacilityCount { get; set; }
    }
}
