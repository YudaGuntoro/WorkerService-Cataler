using Dapper;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using WorkerService.Domains.Dtos;
using WorkerService.Domains.Models;
using WorkerService.MQTT.Clients;
using WorkerService.MQTT.Handlers.SQL;
using WorkerService.MQTT.Interfaces;
using WorkerService.Singletone;

namespace WorkerService.MQTT.Handlers
{
    public class Machine1Handler : BackgroundService
    {
        private readonly ILogger<Machine1Handler> _logger;
        private readonly IMqttPublisher _mqtt;  
        private readonly MachineQuery _machineQuery;
        private readonly IMapper _mapper;
        private readonly IDatabase _cache;
        Card_No_Details_Dto productionDetailsDto = new Card_No_Details_Dto();
        Machine_Status_Dto machineStatusDto = new Machine_Status_Dto();
        Cycle_Time_Dto cycleTimeDto = new Cycle_Time_Dto();
        Machine_Runtime_Dto machineRuntime = new Machine_Runtime_Dto();
        Timestamp_Captured_Dto timeStamp = new Timestamp_Captured_Dto();

        // Letakkan di dalam class handler kamu (sejajar dengan _cache, _logger, dll)
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> _lineLocks = new();

        public Machine1Handler(
            ILogger<Machine1Handler> logger,
            IMqttPublisher mqtt,
            IMapper mapper,
            MachineQuery machine1Query,
            IConnectionMultiplexer redis   // ✅ inject redis
        )
        {
            _logger = logger;
            _mqtt = mqtt;
            _mapper = mapper;
            _machineQuery = machine1Query;

            // ambil DB default Redis
            _cache = redis?.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task HandleMessageAsync(string topic, string actualMachinePayload, CancellationToken ct = default)
        {
            try
            {
                var machineData = JsonConvert.DeserializeObject<CZEC1_Machine_Data>(actualMachinePayload);
                if (machineData?.CZEC1_Data == null)
                {
                    _logger.LogWarning("[Machine1] CZEC1_Data tidak ditemukan dalam payload.");
                    return;
                }
               
                var now = DateTime.Now;
                var czec1 = machineData.CZEC1_Data;
                var cardNo = czec1.CardNo;
                var lineNo = czec1.LineNo;

                // NORMAL (default)
                var ShiftChecker = await _machineQuery.GetShiftInsideOrGapAsync();
                if (ShiftChecker is not null)
                {
                    var Mode = ShiftChecker.Mode;
                    var NextShift = ShiftChecker.NextCode;
                    var PrevShift = ShiftChecker.PrevCode;
                    var NextStartTimeShift = ShiftChecker.NextStartDateTime;
                    var PrevEndTimeShift = ShiftChecker.PrevEndDateTime;

                    var before = czec1.MCStatus;
                    if (Mode == "GAP" && czec1.MCStatus == 4) // Jika Status CO saat Gap Inject ke SCH.DT
                    {
                        czec1.MCStatus = 2;
                    }

                    _logger.LogInformation("[ShiftCheck] Mode={Mode} Prev={PrevShift} end={PrevEnd:HH:mm} | Next={NextShift} start={NextStart:HH:mm} | MCStatus {Before}->{After}",
                        Mode, PrevShift, PrevEndTimeShift, NextShift, NextStartTimeShift, before, czec1.MCStatus);
                }

                // Tentukan range waktu
                var startTime = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0);  // 12:00
                var endTime = new DateTime(now.Year, now.Month, now.Day, 12, 45, 0); // 12:45

                if (now >= startTime && now <= endTime)
                {
                    // Aksi jika berada di jam 12:00 - 12:45 Inject ke SCH.DT
                    _logger.LogInformation("[TimeCheck] Masuk range 12:00 - 12:45");

                    // Contoh aksi
                    czec1.MCStatus = 2;
                }
                else
                {
                    _logger.LogInformation("[TimeCheck] Di luar range 12:00 - 12:45");
                }

                // RAMADAN
                //var r2 = await _machineQuery.GetShiftInsideOrGapAsync("RAMADAN");

                // Lock per line
                var sem = _lineLocks.GetOrAdd(Convert.ToInt32(lineNo), _ => new SemaphoreSlim(1, 1));
                // 🔽 di sini ganti tunggu lock dengan versi pakai timeout
                if (!await sem.WaitAsync(TimeSpan.FromSeconds(2), ct))
                {
                    _logger.LogWarning("[PCSD] Skip karena lock timeout untuk Line {LineNo}", lineNo);
                    return;
                }

                // <=== MULAI KAWASAN TERKUNCI
                try
                {
                // =========================
                // 1) HITUNG KPI HARIAN DULU (independen dari CardNo)
                // =========================
                // a) Runtime (08:00–08:00)
                    DateTime date = (now.Hour < 8) ? now.Date.AddDays(-1) : now.Date;
                    var rt = await _machineQuery.GetMachineRuntimeSummaryAsync(lineNo, date);

                    machineRuntime.OFF = rt.Where(x => x.StatusLabel == "OFF").Select(x => x.TotalRuntime).FirstOrDefault();
                    machineRuntime.RUNNING = rt.Where(x => x.StatusLabel == "RUN").Select(x => x.TotalRuntime).FirstOrDefault();
                    machineRuntime.MALFUNCTION = rt.Where(x => x.StatusLabel == "MALFUNCTION").Select(x => x.TotalRuntime).FirstOrDefault();
                    machineRuntime.SCHDT = rt.Where(x => x.StatusLabel == "SCH.DT").Select(x => x.TotalRuntime).FirstOrDefault();
                    machineRuntime.CO = rt.Where(x => x.StatusLabel == "C.O.").Select(x => x.TotalRuntime).FirstOrDefault();

                    machineRuntime.OFF_MIN = rt.Where(x => x.StatusLabel == "OFF").Select(x => x.TotalRuntime.TotalMinutes).FirstOrDefault();
                    machineRuntime.RUN_MIN = rt.Where(x => x.StatusLabel == "RUN").Select(x => x.TotalRuntime.TotalMinutes).FirstOrDefault();
                    machineRuntime.MALFUNCTION_MIN = rt.Where(x => x.StatusLabel == "MALFUNCTION").Select(x => x.TotalRuntime.TotalMinutes).FirstOrDefault();
                    machineRuntime.SCHDT_MIN = rt.Where(x => x.StatusLabel == "SCH.DT").Select(x => x.TotalRuntime.TotalMinutes).FirstOrDefault();
                    machineRuntime.CO_MIN = rt.Where(x => x.StatusLabel == "C.O.").Select(x => x.TotalRuntime.TotalMinutes).FirstOrDefault();

                    timeStamp.SystemTimestamp = now;
                    timeStamp.GatewayTimestamp = machineData.Timestamp;

                    // b) OA% (pakai total break dari DB s/d saat ini)
                    int breakMin = await _machineQuery.GetTotalBreakMinutesAsync();
                    double workingTimeMin = Math.Max(0,
                        (machineRuntime.RUN_MIN + machineRuntime.CO_MIN + machineRuntime.MALFUNCTION_MIN + machineRuntime.SCHDT_MIN) - breakMin);

                    machineStatusDto.OA = (workingTimeMin <= 0)
                        ? 0
                        : Math.Round((machineRuntime.RUN_MIN / workingTimeMin) * 100.0, 2);

                    var oaTarget = await _machineQuery.GetOaTargetAsync(lineNo);
                    machineStatusDto.OATarget = oaTarget?.target ?? 0;
                    
                    
                    // c) PCSD (pakai Redis) + reset 08:00
                    string prefix = $"pcsd:{lineNo}";
                    string keyLast = $"{prefix}:last_actual";
                    string keyDay = $"{prefix}:pcs_day";
                    string keyLastReset = $"{prefix}:last_reset";
                    string keyLastModel = $"machine:{lineNo}:LastModel";

                    // --- Read from Redis ---
                    var lastModelVal = await _cache.StringGetAsync(keyLastModel);
                    var lastActualVal = await _cache.StringGetAsync(keyLast);
                    var pcsDayVal = await _cache.StringGetAsync(keyDay);
                    var lastResetVal = await _cache.StringGetAsync(keyLastReset);

                    // --- Local vars (pcsDay hanya DIBACA, tidak diubah) ---
                    int lastActual = lastActualVal.HasValue ? (int)lastActualVal : 0;
                    int pcsDay = pcsDayVal.HasValue ? (int)pcsDayVal : 0; // <- tidak disentuh selanjutnya
                    //=================================Ori
                    DateTime lastReset = lastResetVal.HasValue ? DateTime.Parse((string)lastResetVal!) : DateTime.MinValue;

                    int currentActual = Convert.ToInt32(czec1.Actual);
                    // Model sekarang (sesuaikan sumber nama modelmu)
                    string currentModel = (productionDetailsDto?.ProductName ?? string.Empty).Trim();
                    string lastModel = lastModelVal.HasValue ? ((string)lastModelVal!).Trim() : string.Empty;
                    bool modelChanged = !string.Equals(lastModel, currentModel, StringComparison.OrdinalIgnoreCase);

                    // Hindari overwrite ke 0 (reset palsu)
                    if (currentActual > 0)
                    {
                        await _cache.StringSetAsync(keyLast, currentActual);
                        _logger.LogInformation("[PCSD] last_actual={Actual}", currentActual);
                    }
                    else
                    {
                        _logger.LogInformation("[PCSD] currentActual=0 → keep last_actual={Last}", lastActual);
                    }

                    _logger.LogInformation("czec1.Actual = {0} (decimal), currentActual = {1} (int),  currentActual1 = {2} (int)", czec1.Actual, currentActual);

                    var shiftStart = new DateTime(now.Year, now.Month, now.Day, 7, 59, 0); // Reset Jam 7.59
                    if (now >= shiftStart && lastReset < shiftStart)
                    {
                        pcsDay = 0;
                        lastActual = currentActual;
                        await _cache.StringSetAsync(keyLastReset, now.ToString("O"));
                        await _cache.StringSetAsync(keyLast, pcsDay);
                        _logger.LogInformation("[PCSD] Reset harian pada {Time}", now);
                    }

                    // Update total pcs harian:
                    // - Jika counter mesin (currentActual) lebih besar atau sama dengan counter sebelumnya (lastActual),
                    //   berarti counter berjalan normal → tambahkan selisih (currentActual - lastActual).
                    // - Jika counter lebih kecil dari lastActual, berarti counter reset (misalnya power off / shift baru),
                    //   maka gunakan nilai currentActual langsung sebagai tambahan.

                    // Update total pcs harian (tetap)
                    pcsDay += (currentActual >= lastActual) ? (currentActual - lastActual) : currentActual;

                    // Simpan nilai utama
                    await _cache.StringSetAsync(keyLast, currentActual);
                    await _cache.StringSetAsync(keyDay, pcsDay);

                    machineStatusDto.PCSD = pcsDay;

                    // e) Progress & target harian by line (bukan by card)
                    var pcsdTarget = await _machineQuery.GetTodayPlanTotalByLineNoAsync(Convert.ToInt16(lineNo));
                    machineStatusDto.PCSDTarget = pcsdTarget;
          
                    if (pcsdTarget <= 0 || pcsDay <= 0) machineStatusDto.Progress = 0;
                    else
                    {
                        var progress = (decimal)pcsDay / pcsdTarget * 100m;
                        machineStatusDto.Progress = Math.Clamp(progress, 0m, 100m);
                    }

                    // f) OK/NG/COCount (OK/NG dari payload; COCount by line)
                    machineStatusDto.OKProduct = czec1.OKCount;
                    machineStatusDto.NGProduct = czec1.NGCount;
                    machineStatusDto.COCount = await _machineQuery.GetCOCountAsync(Convert.ToInt16(lineNo));

                    // g) StatusCode dari MCStatus (tetap isi)
                    var status = await _machineQuery.GetMachineStatusCodeAsync(czec1.MCStatus);
                    if (status is not null)
                    {
                        _mapper.Map(status, machineStatusDto);
                        machineStatusDto.StatusCode = status.StatusLabel ?? "-";
                    }
                    // =========================
                    // 2) CEK CARDNO (hanya mempengaruhi PCSHTarget & ProductionDetails)
                    // =========================
                    // Default: PCSHTarget null
                    machineStatusDto.PCSHTarget = null;

                    if (cardNo <= 100)
                    {
                        machineStatusDto.PCSHTarget = 0;

                        // ProductionDetails minimal
                        productionDetailsDto ??= new Card_No_Details_Dto();
                        productionDetailsDto.LineNo = lineNo;
                        productionDetailsDto.LineName ??= await _machineQuery.GetLineNameByLineNoAsync(Convert.ToInt32(lineNo))?? $"LINE-{lineNo}";
                        productionDetailsDto.SystemPlan = czec1.Plan;
                        productionDetailsDto.MachinePlan = czec1.Plan;
                    }

                    var cardNoParam = await _machineQuery.GetCardNoParameterAsync(cardNo, lineNo);
                    if (cardNoParam is null)
                    {
                        machineStatusDto.StatusCode ??= "Card No Not Found";
                        machineStatusDto.PCSHTarget = 0;

                        // DTO dijamin non-null
                        productionDetailsDto ??= new Card_No_Details_Dto();

                        // isi default semuanya dengan 0 atau string kosong
                        productionDetailsDto.LineNo = lineNo;               // tetap isi LineNo biar traceable
                        productionDetailsDto.LineName ??= await _machineQuery.GetLineNameByLineNoAsync(Convert.ToInt32(lineNo))?? $"LINE-{lineNo}";
                        productionDetailsDto.CardNo = cardNo;
                        productionDetailsDto.ProductName = "No Production";
                        productionDetailsDto.MaterialName = "No Production";
                        productionDetailsDto.PartNo = 0;
                        productionDetailsDto.MaterialNo = 0;
                        productionDetailsDto.SubstrateName = "No Production";
                        productionDetailsDto.TactTime = 0;
                        productionDetailsDto.PassHour = 0;
                        productionDetailsDto.CoatWidthMin = 0;
                        productionDetailsDto.CoatWidthTarget = 0;
                        productionDetailsDto.CoatWidthMax = 0;
                        productionDetailsDto.SolidityMin = 0;
                        productionDetailsDto.SolidityTarget = 0;
                        productionDetailsDto.SolidityMax = 0;
                        productionDetailsDto.Viscosity100Min = 0;
                        productionDetailsDto.Viscosity100Max = 0;
                        productionDetailsDto.Viscosity1Min = 0;
                        productionDetailsDto.Viscosity1Max = 0;
                        productionDetailsDto.PHMin = 0;
                        productionDetailsDto.PHMax = 0;

                        machineStatusDto.PCSHTarget = 0;

                        // dari mesin → tetap pakai nilai actual
                        productionDetailsDto.ActualProduction = czec1.Actual;
                        productionDetailsDto.MachinePlan = czec1.Plan;
                        productionDetailsDto.SystemPlan = 0; // kalau cardNo gak ada, plan otomatis 0

                        _logger.LogWarning(
                            "[Machine1] CardNoParameters NULL untuk CardNo={CardNo}, LineNo={LineNo}. Payload full dipublish dengan nilai default 0.",
                            cardNo, lineNo);
                    }
                    else
                    {
                        // CARDNO VALID → proses seperti biasa (kode kamu yang sudah ada)
                        machineStatusDto.PCSHTarget = cardNoParam.PassHour * 2;
                        productionDetailsDto = _mapper.Map<Card_No_Details_Dto>(cardNoParam);
                        _mapper.Map(czec1, productionDetailsDto);
                        productionDetailsDto.ActualProduction = czec1.Actual;
                    }
                    
                    _logger.LogWarning("[PCSD] Actual Prod {1}", czec1.Actual);

                        // (contoh: map plan sistem harian)
                    var today = DateTime.Today;
                    var lineName = productionDetailsDto.LineName ?? await _machineQuery.GetLineNameByLineNoAsync(Convert.ToInt32(lineNo));

                    // hasil akhir dijamin non-null
                    var productName = cardNoParam?.ProductName?.Trim() ?? productionDetailsDto?.ProductName?.Trim()?? "UNKNOWN";
                    if (string.IsNullOrWhiteSpace(productName)) productName = "UNKNOWN";

                    // === [NEW] Per-Model baseline + PCSH berbasis runtime per-model ===

                    // Step 1: update baseline bila terdeteksi ganti model (hanya saat RUN)
                    await MarkStartRunTimeOnModelChangeAsync(
                        Convert.ToInt32(lineNo),
                        productName,          // nama model final
                        Convert.ToInt32(czec1.MCStatus),       // ← angka dari mesin (RUN = 1)
                        currentActual,
                        ct);

                    // 1) ambil StartRunTime per-model dari Redis
                    var keyStartAt = $"machine:{lineNo}:StartRunTime";  // sesuai key yang kamu pakai
                    var startAtVal = await _cache.StringGetAsync(keyStartAt);

                    DateTime modelStart;
                    if (startAtVal.HasValue &&
                        DateTime.TryParseExact((string)startAtVal!, "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
                    {
                        modelStart = parsed;
                    }
                    else
                    {
                        // fallback aman (misal kalau baseline belum ada)
                        modelStart = DateTime.Now.AddMinutes(-1);
                    }

                    // 2) panggil function kamu (SP: GetMachineRuntimePerModel_MinutesRaw)
                    var perModel = await _machineQuery.GetMachineRuntimePerModelAsync(
                        Convert.ToInt32(lineNo),
                        modelStart,
                        null,   // sampai NOW()
                        ct);

                    // 3) ambil menit RUN
                    var runMin = perModel
                        .Where(r => r.status == 1)
                        .Sum(r => r.totalRuntime);

                    // 4) hitung PCSH per-model (pakai modelActual dari delta counter)
                    var modelActual = currentActual;

                    machineStatusDto.PCSH = (runMin <= 60)
                        ? modelActual
                        : (runMin > 0 ? (int)Math.Round(modelActual * 60.0 / runMin) : 0);

                    if (cardNo > 100) // Jangan Dihapus, harus check diatas 100
                    {
                        // Skip auto-register bila sekarang < 08:00 (pakai 'now' yang sama)
                        // Ambil satu "now" agar konsisten
                        var t = now.TimeOfDay;

                        // true bila di antara 00:00:00 s.d. 07:59:59.999 (sebelum 08:00)
                        bool isMidnightTo0759 = t >= TimeSpan.Zero && t < TimeSpan.FromHours(8);
                        if (isMidnightTo0759)
                        {
                            var shiftDate = isMidnightTo0759 ? now.Date.AddDays(-1) : now.Date; // < 08:00 → kemarin
                            // Selalu auto-register plan dengan shiftDate yang benar
                            var planId = await _machineQuery.AutoRegisterPlan(
                                lineName,
                                productName,
                                DateOnly.FromDateTime(shiftDate),
                                Convert.ToInt32(czec1.Plan),
                                defaultWorkStatusId: 1);

                            // Logging yang tepat (tidak skip)
                            _logger.LogInformation(
                                "[Machine1] Auto-register plan OK. ShiftDate={ShiftDate:yyyy-MM-dd}, Now={Now:yyyy-MM-dd HH:mm}, CardNo={CardNo}, PlanId={PlanId}",
                                shiftDate, now, cardNo, planId);

                            // Nilai aman untuk payload (bisa disesuaikan bila kamu fetch ulang dari DB)
                            productionDetailsDto.SystemPlan = czec1.Plan;
                            productionDetailsDto.MachinePlan = czec1.Plan;
                        }

                        else
                        {
                            // Belum ada plan → coba daftarkan otomatis
                            var planId = await _machineQuery.AutoRegisterPlan(
                                lineName,
                                productName,
                                DateOnly.FromDateTime(DateTime.Now),   // tanggal dari waktu sistem
                                Convert.ToInt32(czec1.Plan),
                                defaultWorkStatusId: 1);

                            if (planId is not null)
                            {
                                // Ambil ulang plan-nya supaya proper untuk mapping (PAKAI TIMESTAMP PENUH)
                                var createdPlan = await _machineQuery.GetPlanAsync(lineName, productName, DateTime.Now);
                                if (createdPlan is not null)
                                {
                                    _mapper.Map(createdPlan, productionDetailsDto);
                                    productionDetailsDto.SystemPlan = createdPlan.PlanQty; // dari DB
                                    productionDetailsDto.MachinePlan = czec1.Plan;         // dari mesin
                                }
                                else
                                {
                                    // fallback (harusnya jarang kejadian)
                                    productionDetailsDto.SystemPlan = czec1.Plan;
                                    productionDetailsDto.MachinePlan = czec1.Plan;
                                }
                            }
                            else
                            {
                                // benar-benar gagal daftar
                                productionDetailsDto.SystemPlan = 0;
                                productionDetailsDto.MachinePlan = czec1.Plan;
                                productionDetailsDto.ProductName = "Today Plan Not Found";
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Machine1] CardNo {CardNo} <= 100, plan tidak diregister. Payload tetap dipublish.",
                            cardNo);
                    }

                    // Mapping Cycle Time
                    var cycleTimeDto = new Cycle_Time_Dto();
                        //cycleTimeDto.Target = czec2Data.COATTarget / 10m;
                        cycleTimeDto.Result = (decimal?)((float)(czec1.COATResult / 10m));
                        cycleTimeDto.Target = (decimal?)((float)(czec1.COATTarget / 10m));
                        cycleTimeDto.A_COAT_1 = (float)czec1.ACOAT1st / 10f;
                        cycleTimeDto.A_COAT_2 = (float)czec1.ACOAT2nd / 10f;
                        cycleTimeDto.A_COAT_3 = (float)czec1.ACOAT3rd / 10f;
                        cycleTimeDto.B_COAT_1 = (float)czec1.BCOAT1st / 10f;
                        cycleTimeDto.B_COAT_2 = (float)czec1.BCOAT2nd / 10f;
                        cycleTimeDto.B_COAT_3 = (float)czec1.BCOAT3rd / 10f;

                    // Insert Ke mesin
                    await _machineQuery.StatusChangeInsertAndUpdateAsync(lineNo, czec1.MCStatus);

                    if (czec1.COATResult == 0)
                    {
                        cycleTimeDto.Judgement = "-";
                    }
                    else
                    {
                        cycleTimeDto.Judgement = czec1.COATResult <= czec1.COATTarget ? "OK" : "NG";
                    }
                        // (opsional) cycle time yang sudah kamu hitung sebelumnya…
                        // cycleTimeDto = ...

                        var payloadFull = new
                        {
                        ProductionDetails = productionDetailsDto,
                        MachineStatus = machineStatusDto,
                        CycleTime = cycleTimeDto,
                        MachineRuntime = machineRuntime,
                        TimeStamp = timeStamp
                    };
                    var jsonFull = JsonConvert.SerializeObject(payloadFull);
                    try { await _mqtt.PublishAsync("machine1/ack", jsonFull); }
                    catch (Exception ex) { _logger.LogError(ex, "[Machine1] Gagal publish ACK(full)"); }
                    }
                    finally // <=== AKHIR KAWASAN TERKUNCI
                    {
                        sem.Release();
                    }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[Machine1] Gagal parsing payload JSON");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Machine1] Terjadi error saat memproses pesan");
            }
        }

        // === [ADD] Per-model keys & helpers ===
        static string KeyLastModel(int lineNo) => $"machine:{lineNo}:LastModel";
        static string KeyStartAt(int lineNo) => $"machine:{lineNo}:StartRunTime";   // "yyyy-MM-dd HH:mm:ss" (local)
        static string KeyStartActual(int lineNo) => $"machine:{lineNo}:StartActual";

        // === [REPLACE ALL] ===
        private async Task MarkStartRunTimeOnModelChangeAsync(
            int lineNo,
            string currentModel,
            int mcStatus,         // gunakan angka dari mesin (RUN = 1)
            int currentActual,
            CancellationToken ct = default)
        {
            // Trigger baseline hanya saat RUN
            if (mcStatus != Mc.RUN) return;
            if (string.IsNullOrWhiteSpace(currentModel)) return;

            currentModel = currentModel.Trim();

            var lastModelVal = await _cache.StringGetAsync(KeyLastModel(lineNo));
            var lastModel = lastModelVal.HasValue ? ((string)lastModelVal!).Trim() : null;

            // Pertama kali: set baseline
            if (string.IsNullOrEmpty(lastModel))
            {
                await _cache.StringSetAsync(KeyLastModel(lineNo), currentModel);
                await _cache.StringSetAsync(KeyStartActual(lineNo), currentActual);
                await _cache.StringSetAsync(KeyStartAt(lineNo), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                _logger.LogInformation("[ModelBaseline] INIT line {Line} model '{Model}' at {Time} actual={Actual}",
                    lineNo, currentModel, DateTime.Now, currentActual);
                return;
            }

            // Ganti model saat RUN → reset baseline
            if (!string.Equals(lastModel, currentModel, StringComparison.OrdinalIgnoreCase))
            {
                var nowLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                await _cache.StringSetAsync(KeyStartActual(lineNo), currentActual);
                await _cache.StringSetAsync(KeyStartAt(lineNo), nowLocal);
                await _cache.StringSetAsync(KeyLastModel(lineNo), currentModel);
                _logger.LogWarning("[ModelBaseline] CHANGED line {Line} '{Old}' → '{New}' at {Time} startActual={Actual}",
                    lineNo, lastModel, currentModel, nowLocal, currentActual);
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Machine1Handler background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ambil payload terakhir dari state
                    var payload = Machine_State.CZEC1Json;
                    if (!string.IsNullOrWhiteSpace(payload))
                    {
                        await HandleMessageAsync("machine1/ack", payload, stoppingToken);
                    }
                    else
                    {
                        _logger.LogDebug("[Machine1Handler] Payload kosong, skip tick {Time}", DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Machine1Handler] Error saat loop publish");
                }

                await Task.Delay(1000, stoppingToken); // publish setiap 1 detik
            }

            _logger.LogInformation("Machine1Handler background service stopped.");
        }
    }
}