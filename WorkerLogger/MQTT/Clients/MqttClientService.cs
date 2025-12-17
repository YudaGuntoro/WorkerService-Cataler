using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WorkerLogger.Domains.Mappings;
using WorkerLogger.Domains.Models;
using WorkerLogger.MQTT.Handlers;
using WorkerLogger.MQTT.Handlers.SQL;
using WorkerLogger.MQTT.Interfaces;
using WorkerLogger.Singletone;

namespace WorkerLogger.MQTT.Clients
{
    public class MqttClientService : BackgroundService, IMqttClientService
    {
        private readonly ILogger<MqttClientService> _logger;
        public readonly IMqttClient _mqttClient = new MqttClientFactory().CreateMqttClient();
        public IMqttClient _mqttClientInstance => _mqttClient; // <-- akses public ke luar
        private MqttClientOptions _mqttClientOptions;
        private readonly IDatabase _cache;

        // ===== Default (const) =====
        private const string Machine1Data = "machine1/ack";
        private const string Machine2Data = "machine2/ack";              // <-- baru
        private const string TopicLogMachineAlarm1 = "/MachineAlarm1";
        private const string TopicLogMachineAlarm2 = "/MachineAlarm2";   // <-- baru
        private const string DefaultBrokerAddress = "broker.hivemq.com";
        private const int DefaultPort = 1883;

        private string _brokerAddress = DefaultBrokerAddress;
        private int _port = DefaultPort;

        // akan di-override dari Settings.ini
        private string _machine1Data = Machine1Data;
        private string _machine2Data = Machine2Data;                 // <-- baru
        private string _topicLogMachineAlarm1 = TopicLogMachineAlarm1;
        private string _topicLogMachineAlarm2 = TopicLogMachineAlarm2; // <-- baru

        private readonly MachineQuery _MachineQuery;

        private MachineDataModel machine1Data = new MachineDataModel();
        private MachineDataModel machine2Data = new MachineDataModel();

        // track status terakhir per (lineNo|message)
        private static readonly ConcurrentDictionary<string, string> _lastAlarmStatus
            = new(StringComparer.OrdinalIgnoreCase);

        int lineNo;
        int lineMasterId;
        int cardNo;
        int targetDay;

        public MqttClientService(ILogger<MqttClientService> logger, MachineQuery MachineQuery, IConnectionMultiplexer redis)
        {
            _logger = logger;
            _MachineQuery = MachineQuery;
            // ambil DB default Redis
            _cache = redis?.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
            _brokerAddress = DefaultBrokerAddress;
            _port = DefaultPort;
            _machine1Data = Machine1Data;
            _machine2Data = Machine2Data;                 // <-- baru
            _topicLogMachineAlarm1 = TopicLogMachineAlarm1;
            _topicLogMachineAlarm2 = TopicLogMachineAlarm2; // <-- baru

            // override dari Settings.ini (kalau ada)
            var cfg = Config.Instance;
            _brokerAddress = cfg.Read("Host", "MQTT") ?? _brokerAddress;
            _port = cfg.ReadInt("Port", "MQTT", _port);
            _machine1Data = cfg.Read("Machine1Data", "MQTT") ?? _machine1Data;
            _machine2Data = cfg.Read("Machine2Data", "MQTT") ?? _machine2Data; // <-- baru
            _topicLogMachineAlarm1 = cfg.Read("TopicLogMachineAlarm1", "MQTT") ?? _topicLogMachineAlarm1;
            _topicLogMachineAlarm2 = cfg.Read("TopicLogMachineAlarm2", "MQTT") ?? _topicLogMachineAlarm2; // <-- baru

            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage.Payload;
                var payloadText = payload.Length > 0 ? Encoding.UTF8.GetString(payload) : string.Empty;

                // tanpa refactor besar: tetap pakai switch, tapi tambahkan case untuk mesin 2
                switch (topic)
                {
                    // mesin 1 (tetap)
                    case Machine1Data:
                    case var t when t == _machine1Data: // dukung override dari Settings.ini
                        await HandleActMachine1Async(payloadText, topic);
                        break;

                    // mesin 2 (baru)
                    case Machine2Data:
                    case var t when t == _machine2Data:
                        await HandleActMachine2Async(payloadText, topic);
                        break;

                    // alarm m1 (tetap)
                    case TopicLogMachineAlarm1:
                    case var t when t == _topicLogMachineAlarm1:
                        await LogAlarmMachine1Async(payloadText, topic);
                        break;

                    // alarm m2 (baru)
                    case TopicLogMachineAlarm2:
                    case var t when t == _topicLogMachineAlarm2:
                        await LogAlarmMachine2Async(payloadText, topic);
                        break;

                    default:
                        _logger.LogWarning($"[MQTT] Topik tidak dikenali: {topic}");
                        break;
                }
            };

            _mqttClient.ConnectedAsync += async e =>
            {
                _logger.LogInformation("Terhubung ke MQTT broker.");
                // setiap kali connected (termasuk reconnect), pastikan subscribe ulang
                await SubscribeAllAsync();
                await Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("Terputus dari MQTT broker. Mencoba reconnect dalam 5 detik...");
                await Task.Delay(TimeSpan.FromSeconds(5));
                try
                {
                    if (_mqttClientOptions != null) // ✅ guard
                        await _mqttClient.ConnectAsync(_mqttClientOptions);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Gagal reconnect: {ex.Message}");
                }
            };
        }

        private async Task HandleActMachine1Async(string payload, string topic)
        {
            try
            {
                // ====== 1) Deserialisasi JSON yang toleran ======
                // - Ignore property yang tidak dikenal
                // - Biarkan nilai null tetap null (untuk kita cek)
                // - Tangkap error per-field agar proses tidak gagal total
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    Error = (sender, args) =>
                    {
                        _logger.LogWarning("[ActMachine1] JSON field error at {Path}: {Message}",
                            args.ErrorContext.Path, args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true; // tandai error sudah ditangani
                    }
                };

                // Deserialisasi payload ke model utama
                machine1Data = JsonConvert.DeserializeObject<MachineDataModel>(payload, settings);

                // Validasi minimal: TimeStamp wajib ada
                if (machine1Data?.TimeStamp == null)
                {
                    _logger.LogWarning("[ActMachine1] Data tidak lengkap. TimeStamp null.");
                    return;
                }

                // ====== 2) Anti-NRE: pastikan sub-objek tidak null ======
                machine1Data.ProductionDetails ??= new ProductionDetails();
                machine1Data.CycleTime ??= new CycleTime();

                // Ambil field penting dengan fallback string.Empty agar aman dipakai
                var lineName = machine1Data.ProductionDetails.LineName ?? string.Empty;
                var productName = machine1Data.ProductionDetails.ProductName ?? string.Empty;

                // Konversi aman LineNo (bisa nullable/decimal) -> int
                int lineNo = Convert.ToInt32(machine1Data.ProductionDetails.LineNo);

                _logger.LogInformation("[ActMachine1] Memproses LineNo: {lineNo}", lineNo);

                // ====== 3) Simpan CycleTime ======
                // Catatan: object CycleTime sudah dijamin non-null di atas
                await _MachineQuery.InsertCycleTimeAsync(
                    machine1Data.ProductionDetails,
                    machine1Data.CycleTime,
                    machine1Data.TimeStamp
                );

                // ====== 4) Upsert Production History (read-only guard via Redis) ======

                // ActualProduction dari decimal? -> int (dibulatkan ke bawah), minimal 0
                var actualProd = machine1Data.ProductionDetails.ActualProduction ?? 0m;
                int qtyInt = (int)Math.Max(0, Math.Floor((double)actualProd));

                // Pakai full timestamp untuk aturan hari produksi 08:00–07:59
                var planDateTime = machine1Data.TimeStamp.SystemTimestamp;

                // Cek LineName & ProductName harus ada untuk mencari plan
                if (string.IsNullOrWhiteSpace(lineName) || string.IsNullOrWhiteSpace(productName))
                {
                    _logger.LogWarning("[ActMachineX] Skip upsert: LineName/ProductName kosong. Date={Date}", planDateTime.Date);
                    return;
                }

                // Ambil plan berdasarkan line, product, dan waktu (memperhatikan window 08:00–07:59)
                var plan = await _MachineQuery.GetPlanAsync(lineName, productName, planDateTime);
                if (plan == null || plan.Id <= 0)
                {
                    _logger.LogWarning(
                        "[ActMachineX] Skip upsert history: Plan not found/invalid (Line={Line}, Product={Product}, Date={Date})",
                        lineName, productName, planDateTime.Date
                    );
                    return;
                }

                // Pastikan product yang datang sama dengan ProductName di plan
                if (!string.Equals(plan.ProductName?.Trim(), productName?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "[ActMachineX] Skip upsert: Product mismatch. Plan.ProductName={PlanProd}, Incoming={IncomingProd}, PlanId={PlanId}",
                        plan.ProductName, productName, plan.Id);
                    return;
                }

                // ====== 5) Read-only guard dari Redis (hindari data mundur/duplikat) ======
                var keyLastActual = $"pcsd:{lineNo}:last_actual";
                int? lastActual = null;

                try
                {
                    // Baca baseline qty terakhir (jika ada)
                    var lastActualVal = await _cache.StringGetAsync(keyLastActual);
                    if (lastActualVal.HasValue && int.TryParse((string)lastActualVal!, out var parsed))
                        lastActual = parsed;
                }
                catch (Exception ex)
                {
                    // Jika Redis bermasalah, lanjutkan proses tanpa guard (jangan block produksi)
                    _logger.LogWarning(ex, "[ActMachine 1] Gagal baca Redis {Key}; lanjut tanpa guard.", keyLastActual);
                }

                // Jika punya baseline:
                if (lastActual.HasValue)
                {
                    // a) Jika qty sekarang lebih kecil dari terakhir → skip (anggap noise/reset)
                    if (qtyInt < lastActual.Value)
                    {
                        _logger.LogInformation(
                            "[ActMachine 1] Skip upsert: Qty turun. Last={Last}, Now={Now}, PlanId={PlanId}",
                            lastActual.Value, qtyInt, plan.Id);
                        // (Tidak return di sini dulunya? Jika ingin benar-benar skip, tambahkan return)
                    }

                    // b) Opsional: jika qty sama → skip untuk kurangi write yang berulang
                    if (qtyInt == lastActual.Value)
                    {
                        _logger.LogDebug(
                            "[ActMachine 1] Skip upsert: Qty sama ({Val}). PlanId={PlanId}",
                            qtyInt, plan.Id);
                        // (Sama seperti di atas: tambahkan return jika ingin benar-benar skip)
                    }
                }

                // ====== 6) Upsert ke production_history ======
                // Catatan: di sini tidak menulis balik ke Redis (read-only guard saja)

                if (qtyInt != 0)
                {
                    await _MachineQuery.UpsertProductionHistoryAsync(plan.Id, qtyInt);

                    _logger.LogInformation(
                        "[ActMachine 1] Overwrite production history. PlanId={PlanId}, Qty={Qty}",
                        plan.Id, qtyInt
                    );
                }
                else
                {
                    _logger.LogInformation(
                   "[ActMachine 1] No Overwrite production history. PlanId={PlanId}, Qty={Qty}, Should not be 0",
                   plan.Id, qtyInt);
                }

           

            }
            catch (Exception ex)
            {
                // Error handler utama agar proses tidak mematikan service
                _logger.LogError(ex, "[ActMachine1] Gagal memproses payload.");
            }
        }

        private async Task HandleActMachine2Async(string payload, string topic)
        {
            try
            {
                // ====== 1) Deserialisasi JSON yang toleran ======
                // - Ignore property yang tidak dikenal
                // - Biarkan nilai null tetap null (untuk kita cek)
                // - Tangkap error per-field agar proses tidak gagal total
                var settings = new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    Error = (sender, args) =>
                    {
                        _logger.LogWarning("[ActMachine2] JSON field error at {Path}: {Message}",
                            args.ErrorContext.Path, args.ErrorContext.Error.Message);
                        args.ErrorContext.Handled = true; // tandai error sudah ditangani
                    }
                };

                // Deserialisasi payload ke model utama
                machine2Data = JsonConvert.DeserializeObject<MachineDataModel>(payload, settings);

                // Validasi minimal: TimeStamp wajib ada
                if (machine2Data?.TimeStamp == null)
                {
                    _logger.LogWarning("[ActMachine2] Data tidak lengkap. TimeStamp null.");
                    return;
                }

                // ====== 2) Anti-NRE: pastikan sub-objek tidak null ======
                machine2Data.ProductionDetails ??= new ProductionDetails();
                machine2Data.CycleTime ??= new CycleTime();

                // Ambil field penting dengan fallback string.Empty agar aman dipakai
                var lineName = machine2Data.ProductionDetails.LineName ?? string.Empty;
                var productName = machine2Data.ProductionDetails.ProductName ?? string.Empty;

                // Konversi aman LineNo (bisa nullable/decimal) -> int
                int lineNo = Convert.ToInt32(machine2Data.ProductionDetails.LineNo);

                _logger.LogInformation("[ActMachine2] Memproses LineNo: {lineNo}", lineNo);

                // ====== 3) Simpan CycleTime ======
                // Catatan: object CycleTime sudah dijamin non-null di atas
                await _MachineQuery.InsertCycleTimeAsync(
                    machine2Data.ProductionDetails,
                    machine2Data.CycleTime,
                    machine2Data.TimeStamp
                );

                // ====== 4) Upsert Production History (read-only guard via Redis) ======

                // ActualProduction dari decimal? -> int (dibulatkan ke bawah), minimal 0
                var actualProd = machine2Data.ProductionDetails.ActualProduction ?? 0m;
                int qtyInt = (int)Math.Max(0, Math.Floor((double)actualProd));

                // Pakai full timestamp untuk aturan hari produksi 08:00–07:59
                var planDateTime = machine2Data.TimeStamp.SystemTimestamp;

                // Cek LineName & ProductName harus ada untuk mencari plan
                if (string.IsNullOrWhiteSpace(lineName) || string.IsNullOrWhiteSpace(productName))
                {
                    _logger.LogWarning("[ActMachine2] Skip upsert: LineName/ProductName kosong. Date={Date}", planDateTime.Date);
                    return;
                }

                // Ambil plan berdasarkan line, product, dan waktu (memperhatikan window 08:00–07:59)
                var plan = await _MachineQuery.GetPlanAsync(lineName, productName, planDateTime);
                if (plan == null || plan.Id <= 0)
                {
                    _logger.LogWarning(
                        "[ActMachine2] Skip upsert history: Plan not found/invalid (Line={Line}, Product={Product}, Date={Date})",
                        lineName, productName, planDateTime.Date
                    );
                    return;
                }

                // Pastikan product yang datang sama dengan ProductName di plan
                if (!string.Equals(plan.ProductName?.Trim(), productName?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "[ActMachine2] Skip upsert: Product mismatch. Plan.ProductName={PlanProd}, Incoming={IncomingProd}, PlanId={PlanId}",
                        plan.ProductName, productName, plan.Id);
                    return;
                }

                // ====== 5) Read-only guard dari Redis (hindari data mundur/duplikat) ======
                var keyLastActual = $"pcsd:{lineNo}:last_actual";
                int? lastActual = null;

                try
                {
                    // Baca baseline qty terakhir (jika ada)
                    var lastActualVal = await _cache.StringGetAsync(keyLastActual);
                    if (lastActualVal.HasValue && int.TryParse((string)lastActualVal!, out var parsed))
                        lastActual = parsed;
                }
                catch (Exception ex)
                {
                    // Jika Redis bermasalah, lanjutkan proses tanpa guard (jangan block produksi)
                    _logger.LogWarning(ex, "[ActMachine2] Gagal baca Redis {Key}; lanjut tanpa guard.", keyLastActual);
                }

                // Jika punya baseline:
                if (lastActual.HasValue)
                {
                    // a) Jika qty sekarang lebih kecil dari terakhir → skip (anggap noise/reset)
                    if (qtyInt < lastActual.Value)
                    {
                        _logger.LogInformation(
                            "[ActMachine2] Skip upsert: Qty turun. Last={Last}, Now={Now}, PlanId={PlanId}",
                            lastActual.Value, qtyInt, plan.Id);
                        // (Tambah 'return' jika ingin benar-benar skip)
                    }

                    // b) Opsional: jika qty sama → skip untuk kurangi write yang berulang
                    if (qtyInt == lastActual.Value)
                    {
                        _logger.LogDebug(
                            "[ActMachine2] Skip upsert: Qty sama ({Val}). PlanId={PlanId}",
                            qtyInt, plan.Id);
                        // (Tambah 'return' jika ingin benar-benar skip)
                    }
                }

                if (qtyInt != 0)
                {
                    // ====== 6) Upsert ke production_history ======
                    // Catatan: di sini tidak menulis balik ke Redis (read-only guard saja)
                    await _MachineQuery.UpsertProductionHistoryAsync(plan.Id, qtyInt);

                    _logger.LogInformation(
                   "[ActMachine2] Overwrite production history. PlanId={PlanId}, Qty={Qty}",
                   plan.Id, qtyInt);
                }
                else
                {
                    _logger.LogInformation(
                   "[ActMachine2] No Overwrite production history. PlanId={PlanId}, Qty={Qty}, Should not be 0",
                   plan.Id, qtyInt);
                }
            }
            catch (Exception ex)
            {
                // Error handler utama agar proses tidak mematikan service
                _logger.LogError(ex, "[ActMachine2] Gagal memproses payload.");
            }
        }

        private Task LogAlarmMachine1Async(string payload, string topic) => LogAlarmCommonAsync(payload, topic, lineNo: 5041, machineKey: "M1", logTag: "AlarmMachine1");
        private Task LogAlarmMachine2Async(string payload, string topic) => LogAlarmCommonAsync(payload, topic, lineNo: 5042, machineKey: "M2", logTag: "AlarmMachine2");

        public void Configure(string brokerHost, int brokerPort)
        {
            var host = string.IsNullOrWhiteSpace(brokerHost) ? DefaultBrokerAddress : brokerHost;
            var port = brokerPort > 0 ? brokerPort : DefaultPort;

            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId("WorkerLogger")
                .WithTcpServer(host, port)
                .WithCleanSession(false) // <— penting: simpan session & subscriptions
                                         //.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500) // jika pakai MQTT v5
                                         //.WithSessionExpiryInterval(uint.MaxValue) // jika v5: persist session
                .Build();
        }

        private async Task SubscribeAllAsync(CancellationToken ct = default)
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogWarning("[MQTT] Skip SubscribeAll: client belum connected.");
                return;
            }

            var subOpts = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(_machine1Data).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .WithTopicFilter(f => f.WithTopic(_topicLogMachineAlarm1).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .WithTopicFilter(f => f.WithTopic(_machine2Data).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .WithTopicFilter(f => f.WithTopic(_topicLogMachineAlarm2).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .Build();

            await _mqttClient.SubscribeAsync(subOpts, ct);
            _logger.LogInformation("[MQTT] Subscribed (all) -> {T1}, {T2}, {T3}, {T4}",
                _machine1Data, _topicLogMachineAlarm1, _machine2Data, _topicLogMachineAlarm2);
        }


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

        public async Task SubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            if (!_mqttClient.IsConnected)
                throw new InvalidOperationException("Client belum terhubung.");

            await _mqttClient.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce, cancellationToken);
            _logger.LogInformation($"Subscribed ke topic '{topic}'.");
        }

        public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
        {
            if (!_mqttClient.IsConnected)
                throw new InvalidOperationException("Client belum terhubung.");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await _mqttClient.PublishAsync(message, cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // ✅ pastikan ada nilai default kalau belum di-set dari luar
            if (string.IsNullOrWhiteSpace(_brokerAddress)) _brokerAddress = DefaultBrokerAddress;
            if (_port <= 0) _port = DefaultPort;
            Configure(_brokerAddress, _port);
            await ConnectAsync(stoppingToken);
            // subscribe semua topik sekali di awal
            await SubscribeAllAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_mqttClient.IsConnected)
                {
                    _logger.LogWarning("[MQTT] Terputus. Mencoba reconnect...");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    await ConnectAsync(stoppingToken);

                    if (_mqttClient.IsConnected)
                    {
                        _logger.LogInformation("[MQTT] Reconnect sukses. Subscribe ulang (all).");
                        await SubscribeAllAsync(stoppingToken);
                    }
                }
                else
                {
                    // ✅ null-safety saat akses state — Mesin 1
                    if (machine1Data?.ProductionDetails != null && machine1Data.MachineStatus != null)
                    {
                        var currenttarget = await _MachineQuery.GetCurrentTargetAsync(lineMasterId: 1, nowLocal: DateTime.Now);
                        await InsertProductionLogIfOClockAsync(
                            lineMasterId: 1,
                            cardNo: Convert.ToInt16(machine1Data.ProductionDetails.LineNo),
                            target: currenttarget,
                            actual: Convert.ToInt16(machine1Data.MachineStatus.PCSD),
                            isMachine2: false
                        );
                    }

                    // ✅ null-safety — Mesin 2
                    if (machine2Data?.ProductionDetails != null && machine2Data.MachineStatus != null)
                    {
                        var currenttarget = await _MachineQuery.GetCurrentTargetAsync(lineMasterId: 2, nowLocal: DateTime.Now);
                        await InsertProductionLogIfOClockAsync(
                            lineMasterId: 2, // ganti sesuai mapping
                            cardNo: Convert.ToInt16(machine2Data.ProductionDetails.LineNo),
                            target: currenttarget,
                            actual: Convert.ToInt16(machine2Data.MachineStatus.PCSD),
                            isMachine2: true
                        );
                    }

                    // Contoh publish dummy
                    await PublishAsync("test/topic", "Hello from BackgroundService", stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        // pisahkan lastInsert per mesin supaya keduanya bisa insert tiap jam
        private DateTime _lastInsertM1 = DateTime.MinValue;
        private DateTime _lastInsertM2 = DateTime.MinValue;

        public async Task InsertProductionLogIfOClockAsync(int lineMasterId, int? cardNo, int target, int actual, bool isMachine2)
        {
            try
            {
                var now = DateTime.Now;

                var last = isMachine2 ? _lastInsertM2 : _lastInsertM1;
                if (now.Minute == 0 && last.Hour != now.Hour)
                {
                    if (isMachine2) _lastInsertM2 = now; else _lastInsertM1 = now;

                    await _MachineQuery.InsertProductionCountLogAsync(lineMasterId, cardNo, target, actual, now);

                    _logger.LogInformation(
                        "[ProductionLog {M}] Insert hourly log: LineMasterId={LineMasterId}, Target={Target}, Actual={Actual}, CardNo={CardNo}, Time={Time}",
                        isMachine2 ? "M2" : "M1", lineMasterId, target, actual, cardNo, now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductionLog {M}] Gagal insert hourly log untuk LineMasterId={LineMasterId}",
                    isMachine2 ? "M2" : "M1", lineMasterId);
            }
        }

        // ==== COMMON ====
        private async Task LogAlarmCommonAsync(string payload, string topic, int lineNo, string machineKey, string logTag)
        {
            try
            {
                // Parse payload
                var alarm = JsonConvert.DeserializeObject<Alarm_Log_Payload>(payload);
                if (alarm == null)
                {
                    _logger.LogWarning("[{Tag}] Payload null/invalid. Topic={Topic}, Payload={Payload}", logTag, topic, payload);
                    return;
                }

                // Normalisasi data
                var status = (alarm.Status ?? "").Trim().ToLowerInvariant();
                var message = string.IsNullOrWhiteSpace(alarm.Message) ? "Unknown alarm" : alarm.Message.Trim();

                // Kunci de-dupe per mesin|line|pesan
                var key = $"{machineKey}|{lineNo}|{message}";

                // Recovered → tandai & selesai (no DB insert)
                if (status == "recovered")
                {
                    _lastAlarmStatus[key] = "recovered";
                    _logger.LogInformation("[{Tag}] Recovered (no-op). LineNo={LineNo}, Msg='{Msg}'", logTag, lineNo, message);
                    return;
                }

                // Hanya proses 'triggered'
                if (status != "triggered")
                {
                    _logger.LogWarning("[{Tag}] Status tidak dikenal: '{Status}'. LineNo={LineNo}, Msg='{Msg}'", logTag, alarm.Status, lineNo, message);
                    return;
                }

                // Skip jika sudah pernah 'triggered' untuk key yang sama
                if (_lastAlarmStatus.TryGetValue(key, out var last) &&
                    string.Equals(last, "triggered", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("[{Tag}] Skip insert (sudah triggered sebelumnya). LineNo={LineNo}, Msg='{Msg}'", logTag, lineNo, message);
                    return;
                }

                // (Optional) waktu dari payload bila ingin dipakai/log
                var when = alarm.Ts;

                // Waktu insert pakai sekarang
                var timestamp = DateTime.Now;

                // Insert DB
                var newId = await _MachineQuery.InsertAlarmLogAsync(message, lineNo, timestamp);

                // Tandai status terakhir
                _lastAlarmStatus[key] = "triggered";

                // Log hasil
                _logger.LogInformation("[{Tag}] INSERT OK. ID={Id}, LineNo={LineNo}, Msg='{Msg}', Time={Time:o}",
                    logTag, newId, lineNo, message, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Tag}] Gagal memproses payload. Topic={Topic}", logTag, topic);
            }
        }

    }
}
