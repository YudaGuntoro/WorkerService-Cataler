using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerService.Domains.Models;

namespace WorkerService.MQTT.Handlers.Service
{
    public interface ICounterRedisService
    {
        Task SaveAsync(Counter_Snapshot snapshot, CancellationToken ct = default);
        Task<Counter_Snapshot?> LoadAsync(CancellationToken ct = default);
        Task<Counter_Snapshot?> TryApplyInjectionAsync(ILogger logger, CancellationToken ct = default);
    }
}
