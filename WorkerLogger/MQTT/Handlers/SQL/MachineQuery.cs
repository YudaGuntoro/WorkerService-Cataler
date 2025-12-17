using System;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using WorkerLogger.Singletone;
using WorkerLogger.Domains.Mappings;
using WorkerLogger.Domains.Models;

namespace WorkerLogger.MQTT.Handlers.SQL
{
    public class MachineQuery
    {
        private readonly ILogger<MachineQuery> _logger;
        public MachineQuery(ILogger<MachineQuery> logger)
        {
            _logger = logger;
        }
        // ===== 2) Ambil TARGET saja untuk jam saat ini (fallback 0 kalau tidak ada) =====
        public async Task<int> GetCurrentTargetAsync(int lineMasterId, DateTime nowLocal)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);

                var hour = nowLocal.Hour;
                const string sql = @"
                        SELECT target
                        FROM production_count_master
                        WHERE lineMasterId  = @lineMasterId
                          AND operationHour = @hour
                        ORDER BY id DESC
                        LIMIT 1";

                var param = new { lineMasterId, hour };
                var target = await connection.ExecuteScalarAsync<int?>(sql, param);

                if (target is null)
                {
                    _logger.LogWarning("[PCM] Target not found. LineMasterId={Line}, Hour={Hour}. Return 0.", lineMasterId, hour);
                    return 0;
                }

                return target.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PCM] GetCurrentTargetAsync error. LineMasterId={Line}, Now={Now:o}", lineMasterId, nowLocal);
                return 0;
            }
        }
        public async Task InsertCycleTimeAsync(ProductionDetails productionDetails, CycleTime cycle, TimeStamp timeStamp)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();

                var machineId = await connection.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT id FROM line_master WHERE lineNo = @LineNo",
                    new { LineNo = (int)productionDetails.LineNo });

                if (machineId == null)
                {
                    _logger.LogWarning("Machine ID tidak ditemukan untuk LineNo: {LineNo}", productionDetails.LineNo);
                    return;
                }

                const string insertSql = @"
                    INSERT INTO log_cycletime (
                        machineId, target, result, judgement,
                        aCoat1, aCoat2, aCoat3,
                        bCoat1, bCoat2, bCoat3,
                        timestamp, gatewayTimestamp
                    )
                    VALUES (
                        @MachineId, @Target, @Result, @Judgement,
                        @ACoat1, @ACoat2, @ACoat3,
                        @BCoat1, @BCoat2, @BCoat3,
                        @timestamp, @gatewayTimestamp
                    );";

                var parameters = new
                {
                    MachineId = machineId.Value,
                    Target = cycle.Target,
                    Result = cycle.Result,
                    Judgement = cycle.Judgement,
                    ACoat1 = cycle.A_COAT_1,
                    ACoat2 = cycle.A_COAT_2,
                    ACoat3 = cycle.A_COAT_3,
                    BCoat1 = cycle.B_COAT_1,
                    BCoat2 = cycle.B_COAT_2,
                    BCoat3 = cycle.B_COAT_3,
                    timestamp = timeStamp.SystemTimestamp,
                    gatewayTimestamp = timeStamp.GatewayTimestamp,
                };

                await connection.ExecuteAsync(insertSql, parameters);

                _logger.LogInformation("Data CycleTime berhasil disimpan untuk Machine ID: {MachineId}", machineId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menyimpan data CycleTime untuk LineNo: {LineNo}", productionDetails.LineNo);
            }
        }
        // 🚨 Tambahan di sini: insert alarm_log_history
        public async Task<long?> InsertAlarmLogAsync(string message, int lineNo, DateTime timestampUtc)
        {
            try
            {
                const string sql = @"
                    INSERT INTO alarm_log_history (message, lineNo, timestamp)
                    VALUES (@Message, @LineNo, @Timestamp);
                    SELECT LAST_INSERT_ID();";

                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();

                var newId = await connection.ExecuteScalarAsync<long>(sql, new
                {
                    Message = message,
                    LineNo = lineNo,
                    // Simpan UTC agar konsisten (kolom bertipe TIMESTAMP)
                    Timestamp = timestampUtc
                });

                _logger.LogInformation("Alarm log tersimpan. ID: {Id}, LineNo: {LineNo}, Message: {Message}", newId, lineNo, message);
                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menyimpan alarm log untuk LineNo: {LineNo}, Message: {Message}", lineNo, message);
                return null;
            }
        }
        public async Task<long?> InsertProductionCountLogAsync(int lineMasterId,int? cardNo,int target,int actual,DateTime timestamp)
        {
            try
            {
                const string sql = @"
                    INSERT INTO production_count_history 
                        (lineMasterId, cardNo, target, actual, timestamp)
                    VALUES (@LineMasterId, @CardNo, @Target, @Actual, @Timestamp);
                    SELECT LAST_INSERT_ID();";

                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();

                var newId = await connection.ExecuteScalarAsync<long>(sql, new
                {
                    LineMasterId = lineMasterId,
                    CardNo = cardNo,
                    Target = target,
                    Actual = actual,
                    Timestamp = timestamp   // simpan dalam UTC
                });

                _logger.LogInformation(
                    "Production count log tersimpan. ID: {Id}, LineMasterId: {LineMasterId}, Target={Target}, Actual={Actual}, CardNo={CardNo}",
                    newId, lineMasterId, target, actual, cardNo);

                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Gagal menyimpan production count log. LineMasterId={LineMasterId}, Target={Target}, Actual={Actual}, CardNo={CardNo}",
                    lineMasterId, target, actual, cardNo);
                return null;
            }
        }
        public async Task<Production_Plan_Master?> GetPlanAsync(string lineName, string productName, DateTime planDate)
        {
            try
            {
                // Aturan hari kerja: mulai 08:00
                var shiftStart = TimeSpan.FromHours(8);

                // Jika sekarang (<planDate>) masih sebelum 08:00, pakai plan tanggal sebelumnya
                var effectiveDate = (planDate.TimeOfDay < shiftStart)
                    ? planDate.Date.AddDays(-1)
                    : planDate.Date;

                if (effectiveDate != planDate.Date)
                {
                    _logger.LogInformation(
                        "[Plan] {Now:HH:mm} < 08:00 → gunakan plan tanggal sebelumnya: {Eff:yyyy-MM-dd}",
                        planDate, effectiveDate);
                }

                using var connection = new MySqlConnection(dbConfig.MysqlConnString);

                const string sql = @"
            SELECT
              ppm.id          AS Id,
              lm.lineNo       AS LineNo,
              pm.productName  AS ProductName,
              ppm.planDate    AS PlanDate,
              ppm.planQty     AS PlanQty
            FROM production_plan_master AS ppm
            JOIN line_master    AS lm ON lm.id = ppm.lineMasterId
            JOIN product_master AS pm ON pm.id = ppm.productMasterId
            WHERE TRIM(lm.lineName) = TRIM(@lineName)
              AND ppm.planDate      = @planDate
              AND pm.productName    = @productName
            ORDER BY ppm.id ASC
            LIMIT 1;";

                var param = new
                {
                    lineName = lineName?.Trim(),
                    productName = productName?.Trim(),
                    planDate = effectiveDate
                };

                var plan = await connection.QueryFirstOrDefaultAsync<Production_Plan_Master>(sql, param);

                if (plan == null)
                {
                    _logger.LogWarning("[Plan] Not found. Line={Line}, Product={Prod}, Date={Date:yyyy-MM-dd}",
                        lineName, productName, effectiveDate);
                }

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Plan] Error. Line={Line}, Product={Prod}, Date={Date:yyyy-MM-dd}",
                    lineName, productName, planDate);
                return null;
            }
        }

        public async Task<long?> UpsertProductionHistoryAsync(int planId, int qty)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();

                // Cek apakah sudah ada record untuk planId
                var historyId = await connection.QuerySingleOrDefaultAsync<long?>(
                    "SELECT id FROM production_history WHERE productionPlanId = @PlanId LIMIT 1",
                    new { PlanId = planId });

                if (historyId == null)
                {
                    // Belum ada → INSERT
                    const string insertSql = @"
                        INSERT INTO production_history (productionPlanId, actualQty, `timestamp`)
                        VALUES (@PlanId, @Qty, @Now);
                        SELECT LAST_INSERT_ID();";

                    historyId = await connection.ExecuteScalarAsync<long>(insertSql, new
                    {
                        PlanId = planId,
                        Qty = qty,
                        Now = DateTime.Now
                    });

                    _logger.LogInformation(
                        "[UpsertProductionHistory] INSERT OK. PlanId={PlanId}, HistoryId={HistoryId}, Qty={Qty}",
                        planId, historyId, qty);
                }
                else
                {
                    // Sudah ada → UPDATE
                    const string updateSql = @"
                        UPDATE production_history
                        SET actualQty = @Qty,
                            `timestamp` = @Now
                        WHERE productionPlanId = @PlanId";

                    await connection.ExecuteAsync(updateSql, new
                    {
                        PlanId = planId,
                        Qty = qty,
                        Now = DateTime.Now
                    });

                    _logger.LogInformation(
                        "[UpsertProductionHistory] UPDATE OK. PlanId={PlanId}, HistoryId={HistoryId}, Qty={Qty}",
                        planId, historyId, qty);
                }

                return historyId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[UpsertProductionHistory] Gagal INSERT/UPDATE. PlanId={PlanId}, Qty={Qty}",
                    planId, qty);
                return null;
            }
        }
    }
}
