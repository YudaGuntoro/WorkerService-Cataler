using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerService.Domains.Models;

namespace WorkerService.MQTT.Handlers.Service
{
    internal class CounterRedisService : ICounterRedisService
    {

        private readonly IDatabase _db;
        private const string KEY_STATE = "counter:state";
        private const string KEY_INJECT = "counter:inject";

        public CounterRedisService(IConnectionMultiplexer mux)
        {
            _db = mux.GetDatabase();
        }

        public async Task SaveAsync(Counter_Snapshot snapshot, CancellationToken ct = default)
        {
            snapshot.UpdatedAt = DateTime.UtcNow;
            var json = JsonConvert.SerializeObject(snapshot, Formatting.None);
            await _db.StringSetAsync(KEY_STATE, json);
        }

        public async Task<Counter_Snapshot?> LoadAsync(CancellationToken ct = default)
        {
            var val = await _db.StringGetAsync(KEY_STATE);
            if (val.IsNullOrEmpty) return null;

            try
            {
                return JsonConvert.DeserializeObject<Counter_Snapshot>(val!);
            }
            catch
            {
                return null;
            }
        }

        public async Task<Counter_Snapshot?> TryApplyInjectionAsync(ILogger logger, CancellationToken ct = default)
        {
            var entries = await _db.HashGetAllAsync(KEY_INJECT);
            if (entries.Length == 0) return null;

            var apply = entries.FirstOrDefault(e => e.Name == "apply").Value;
            if (apply.IsNullOrEmpty || apply != "1") return null;

            var current = await LoadAsync(ct) ?? new Counter_Snapshot();

            int GetInt(string name, int def)
            {
                var v = entries.FirstOrDefault(e => e.Name == name).Value;
                return v.IsNullOrEmpty ? def : (int.TryParse(v!, out var n) ? n : def);
            }
            DateTime GetDate(string name, DateTime def)
            {
                var v = entries.FirstOrDefault(e => e.Name == name).Value;
                return v.IsNullOrEmpty ? def : (DateTime.TryParse(v!, out var d) ? d : def);
            }

            var injected = new Counter_Snapshot
            {
                OK_M1 = GetInt(nameof(Counter_Snapshot.OK_M1), current.OK_M1),
                NG_M1 = GetInt(nameof(Counter_Snapshot.NG_M1), current.NG_M1),
                Actual_M1 = GetInt(nameof(Counter_Snapshot.Actual_M1), current.Actual_M1),
             
                OK_M2 = GetInt(nameof(Counter_Snapshot.OK_M2), current.OK_M2),
                NG_M2 = GetInt(nameof(Counter_Snapshot.NG_M2), current.NG_M2),
                Actual_M2 = GetInt(nameof(Counter_Snapshot.Actual_M2), current.Actual_M2),
               
                UpdatedAt = DateTime.UtcNow
            };

            await SaveAsync(injected, ct);
            await _db.HashDeleteAsync(KEY_INJECT, "apply"); // reset flag apply

            logger?.LogInformation("[REDIS][INJECT] CounterSnapshot injected and saved @ {Time:u}", injected.UpdatedAt);
            return injected;
        }
    }
}
