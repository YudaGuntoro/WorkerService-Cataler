using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.MQTT.Interfaces
{
    public interface IMqttClientService : IMqttPublisher
    {
        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task SubscribeAsync(string topic, CancellationToken cancellationToken = default);
        void Configure(string brokerHost, int brokerPort);
    }
}