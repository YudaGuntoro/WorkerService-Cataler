using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Persistence.Helper
{
    public interface IDeviceMapProvider
    {
        // contoh: "m1", "m2"
        string MachineKey { get; }

        // peta kategori per device; kalau tidak ada di map, nanti fallback prefix
        IReadOnlyDictionary<string, string> DeviceCategoryMap { get; }
    }
}
