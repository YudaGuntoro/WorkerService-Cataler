using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Persistence.Helper
{
    public class Machine1DeviceMapProvider : IDeviceMapProvider   // <— PUBLIC
    {
        public string MachineKey => "m1";

        public IReadOnlyDictionary<string, string> DeviceCategoryMap { get; } =
            new Dictionary<string, string>
            {
                { "AD01", "VALVE" },
                { "M02",  "ROBOT" },
                { "CYL1", "CY" }
            };
    }

    public class Machine2DeviceMapProvider : IDeviceMapProvider   // <— PUBLIC
    {
        public string MachineKey => "m2";

        public IReadOnlyDictionary<string, string> DeviceCategoryMap { get; } =
            new Dictionary<string, string>
            {
                { "AD11", "VALVE" },
                { "M12",  "ROBOT" },
                { "CYL2", "CY" }
            };
    }
}
