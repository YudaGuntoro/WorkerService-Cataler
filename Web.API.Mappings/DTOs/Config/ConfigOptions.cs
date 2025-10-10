using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.Config
{
    public sealed class MqttSettings
    {
        public string Host { get; set; } = "broker.emqx.io";
        public int Port { get; set; } = 1883;
        public string ClientId { get; set; } = "WorkerService";
        public string TopicMachine1 { get; set; } = "/Act_Machine_1";
        public string TopicCounting { get; set; } = "/counting";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool UseTls { get; set; } = false;
    }

    public sealed class RedisAppOptions
    {
        public int Db { get; set; } = 0;
        public string KeyPrefix { get; set; } = "cataler";
    }

}
