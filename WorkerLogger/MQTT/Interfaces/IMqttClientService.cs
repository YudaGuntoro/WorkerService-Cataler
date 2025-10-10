using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerLogger.MQTT.Interfaces
{
    public interface IMqttClientService
    {
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task SubscribeAsync(string topic, CancellationToken cancellationToken = default);
        Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
        void Configure(string brokerHost, int brokerPort);
        IMqttClient _mqttClientInstance { get; }
    }
}
