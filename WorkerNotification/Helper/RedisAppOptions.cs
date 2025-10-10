using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerNotification.Helper
{
    /// <summary>
    /// Opsi Redis untuk menentukan DB index & prefix key.
    /// </summary>
    public class RedisAppOptions
    {
        public int Db { get; set; } = 0;
        public string KeyPrefix { get; set; } = "cataler";
    }
}
