using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerService.MQTT.Interfaces
{
    public interface IMqttPublisher
    {
        Task PublishAsync(string topic, string payload, bool retain = false, int qos = 0, CancellationToken cancellationToken = default);
    }
}
