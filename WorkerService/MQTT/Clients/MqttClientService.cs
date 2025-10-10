using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using StackExchange.Redis;
using WorkerService.Domains.Models;
using WorkerService.Helper;
using WorkerService.MQTT.Handlers;
using WorkerService.MQTT.Handlers.Service;
using WorkerService.MQTT.Handlers.SQL;
using WorkerService.MQTT.Interfaces;
using WorkerService.Singletone;
using IDatabase = StackExchange.Redis.IDatabase;

namespace WorkerService.MQTT.Clients
{
    public class MqttClientService : BackgroundService, IMqttClientService
    {
        private readonly ILogger<MqttClientService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;        // ✅ resolve handler via scope
        private readonly IDatabase _cache;
        private readonly MachineQuery _machineQuery;
        private readonly ICounterRedisService _counterRepo;

        private readonly IMqttClient _mqttClient;
        private MqttClientOptions _mqttClientOptions;

        // ===== Default (const) =====
        private const string DefaultTopicMachine1 = "/Act_Machine_1";
        private const string DefaultTopicMachine2 = "/Act_Machine_2";
        private const string DefaultBrokerAddress = "127.0.0.1";
        private const int DefaultPort = 1883;

        // ===== Runtime fields (mutable) =====
        private string _topicMachine1;
        private string _topicMachine2;
        private string _brokerAddress;
        private int _port;

        public MqttClientService(ILogger<MqttClientService> logger, MachineQuery machine1Query ,IConnectionMultiplexer redis, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _machineQuery = machine1Query;
            _cache = redis.GetDatabase();
            _scopeFactory = scopeFactory;

            // set default
            _topicMachine1 = DefaultTopicMachine1;
            _topicMachine2 = DefaultTopicMachine2;
            _brokerAddress = DefaultBrokerAddress;
            _port = DefaultPort;

            // override dari Settings.ini (kalau ada)
            var cfg = WorkerService.Singletone.Config.Instance;
            _brokerAddress = cfg.Read("Host", "MQTT") ?? _brokerAddress;
            _port = cfg.ReadInt("Port", "MQTT", _port);
            _topicMachine1 = cfg.Read("TopicMachine1", "MQTT") ?? _topicMachine1;
            _topicMachine2 = cfg.Read("TopicMachine2", "MQTT") ?? _topicMachine2;

            var factory = new MqttClientFactory();

            _mqttClient = factory.CreateMqttClient();
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payloadBytes = e.ApplicationMessage.Payload;
                var payloadText = payloadBytes.Length > 0 ? Encoding.UTF8.GetString(payloadBytes) : string.Empty;

                switch (topic)
                {
                    case var t when t == _topicMachine1:
                        await HandleActMachine1Async(payloadText, topic);
                        break;

                    case var t when t == _topicMachine2:
                        await HandleActMachine2Async(payloadText, topic);
                        break;

                    default:
                        _logger.LogWarning("[MQTT] Topik tidak dikenali: {Topic}", topic);
                        break;
                }
            };

            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("Terhubung ke MQTT broker.");
                await Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("Terputus dari MQTT broker. Mencoba reconnect dalam 5 detik...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    await _mqttClient.ConnectAsync(_mqttClientOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Gagal reconnect: {ex.Message}");
                }
            };
        }
        private async Task HandleActMachine1Async(string payload, string topic, CancellationToken ct = default)
        {
            try
            {
                // 1) Bypass payload asli ke Redis (TTL 5 menit)
                await _cache.StringSetAsync($"cataler:mqtt:data:{topic}", payload);

                Machine_State.CZEC1Json = payload;
                var json = JsonConvert.DeserializeObject<CZEC1_Machine_Data>(payload);

                if (json?.CZEC1_Data != null)
                {
                    var lineNo = json.CZEC1_Data.LineNo;
                    var mcStatus = json.CZEC1_Data.MCStatus;

                    _logger.LogWarning("[ActMachine1] MCStatus = {mcStatus}", mcStatus);
                 
                }
                else
                {
                    _logger.LogWarning("[ActMachine1] Payload tidak valid atau kosong.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ActMachine1] Gagal handle payload.");
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="topic"></param>
        /// <returns></returns>
        private async Task HandleActMachine2Async(string payload, string topic)
        {
            try
            {
                // 1) Bypass payload asli ke Redis (TTL 5 menit)
                await _cache.StringSetAsync($"cataler:mqtt:data:{topic}", payload);

                Machine_State.CZEC2Json = payload;
                var json = JsonConvert.DeserializeObject<CZEC2_Machine_Data>(payload);

                if (json?.CZEC2_Data != null)
                {
                    var lineNo = json.CZEC2_Data.LineNo;
                    var mcStatus = json.CZEC2_Data.MCStatus;

                    _logger.LogWarning("[ActMachine2] MCStatus = {mcStatus}", mcStatus);

                }
                else
                {
                    _logger.LogWarning("[ActMachine2] Payload tidak valid atau kosong.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ActMachine2] Gagal handle payload.");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="brokerHost"></param>
        /// <param name="brokerPort"></param>
        public void Configure(string brokerHost, int brokerPort)
        {
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId("WorkerService")
                .WithTcpServer(brokerHost, brokerPort)
                .Build();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_mqttClientOptions == null)
            {
                _logger.LogError("[MQTT] Client options belum dikonfigurasi.");
                throw new InvalidOperationException("MQTT client options belum dikonfigurasi.");
            }

            if (_mqttClient.IsConnected)
            {
                _logger.LogInformation("[MQTT] Sudah terhubung ke broker, tidak perlu reconnect.");
                return;
            }
            try
            {
                _logger.LogInformation("[MQTT] Mencoba konek ke broker...");
                await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);

                if (_mqttClient.IsConnected)
                    _logger.LogInformation("[MQTT] Berhasil terhubung ke broker.");
                else
                    _logger.LogWarning("[MQTT] Gagal konek (tidak exception tapi masih belum connected).");
            }
            catch (MqttCommunicationException ex)
            {
                _logger.LogError(ex, "[MQTT] Gagal komunikasi dengan broker (mungkin nama host salah atau tidak bisa dijangkau).");
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "[MQTT] Kesalahan socket saat koneksi ke broker.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT] Exception saat mencoba koneksi ke broker.");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task SubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            if (!_mqttClient!.IsConnected)
                throw new InvalidOperationException("Client belum terhubung.");

            await _mqttClient.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);

            _logger.LogInformation($"Subscribed ke topic '{topic}'.");
        }
        //
        // Overload BARU — sesuai IMqttPublisher
        //
        /// <summary>
        /// Publish payload ke MQTT broker
        /// </summary>
        /// <param name="topic">Nama topik</param>
        /// <param name="payload">Isi pesan</param>
        /// <param name="retain">Retain flag (default false)</param>
        /// <param name="qos">Quality of Service (0,1,2)</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task PublishAsync(
            string topic,
            string payload,
            bool retain = false,
            int qos = 0,
            CancellationToken cancellationToken = default)
        {
            if (_mqttClient is null || !_mqttClient.IsConnected)
                throw new InvalidOperationException("Client belum terhubung.");

            var level = qos switch
            {
                0 => MqttQualityOfServiceLevel.AtMostOnce,
                1 => MqttQualityOfServiceLevel.AtLeastOnce,
                2 => MqttQualityOfServiceLevel.ExactlyOnce,
                _ => MqttQualityOfServiceLevel.AtMostOnce
            };

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload ?? string.Empty)
                .WithRetainFlag(retain)
                .WithQualityOfServiceLevel(level)
                .Build();

            await _mqttClient.PublishAsync(message, cancellationToken);
            //_logger.LogInformation("Published ke topic '{Topic}' dengan payload '{Payload}'", topic, payload);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrWhiteSpace(_brokerAddress)) _brokerAddress = DefaultBrokerAddress;
            if (_port <= 0) _port = DefaultPort;

            Configure(_brokerAddress, _port);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_mqttClient.IsConnected)
                {
                    _logger.LogWarning($"[MQTT] Terputus. Mencoba connect broker {_brokerAddress}");
                    try
                    {
                        await ConnectAsync(stoppingToken);
                        if (_mqttClient.IsConnected)
                        {
                            // === Mesin 1 ===
                            try { await SubscribeAsync(_topicMachine1, stoppingToken); }
                            catch (Exception ex) { _logger.LogWarning(ex, "[MQTT] Gagal subscribe {0}", _topicMachine1); }

                            // === Mesin 2 ===
                            try { await SubscribeAsync(_topicMachine2, stoppingToken); }
                            catch (Exception ex) { _logger.LogWarning(ex, "[MQTT] Gagal subscribe {0}", _topicMachine2); }

                            _logger.LogInformation("[MQTT] Connected & subscribed.");
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                    catch (Exception ex) { _logger.LogWarning(ex, "[MQTT] Gagal connect, retry nanti."); }

                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    continue;
                }

                // Connected: kerja normal
                try
                {
                    await PublishAsync("test/topic", "Hello from BackgroundService", cancellationToken: stoppingToken);

                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogWarning(ex, "[MQTT] Gagal publish, akan dicoba lagi."); }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

    }
}