using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using WorkerService.Domains.Dtos;
using WorkerService.Domains.Models;
using WorkerService.MQTT.Clients;
using WorkerService.Singletone;

namespace WorkerService.MQTT.Handlers.SQL
{
    public class MachineQuery
    {
        private readonly ILogger<MachineQuery> _logger;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public MachineQuery(ILogger<MachineQuery> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardNo"></param>
        /// <returns></returns>
        public async Task<Card_No_Master?> GetCardNoParameterAsync(decimal cardNo, decimal lineNo)
        {
            try
            {
                using (var connection = new MySqlConnection(dbConfig.MysqlConnString))
                {
                    string sql = @"
                        SELECT *
                        FROM card_no_master
                        WHERE cardNo = @cardNo And lineNo = @lineNo
                          AND IFNULL(IsDeleted, 0) = 0"; 

                    var cardNoDetails = await connection.QuerySingleOrDefaultAsync<Card_No_Master>(sql, new { cardNo = cardNo, lineNo = lineNo }); // cardNo di DB adalah varchar, jadi kita konversi
                    if (cardNoDetails != null)
                    {
                        _logger.LogInformation($"Data ditemukan untuk CardNo {cardNo}: PartNo = {cardNoDetails.PartNo}, LineNo = {cardNoDetails.LineNo}, Model = {cardNoDetails.ProductName}");
                    }
                    else
                    {
                        _logger.LogWarning($"Tidak ditemukan data untuk CardNo: {cardNo} di LineNo {lineNo}");
                    }
                    return cardNoDetails;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"Gagal mengambil data CardNo {cardNo} di lineNo = {cardNo}");
                return null;
            }
        }

        public async Task<int> GetTotalBreakMinutesAsync()
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                const string sql = @"
                        -- Ambil BreakMin yang sesuai dengan waktu saat ini
                        SELECT t.BreakMin AS TotalBreakMin
                        FROM (
                          SELECT 
                            Id,
                            breakTimeParam,
                            BreakMin,
                            -- Normalisasi: semua jam < 08:00 dianggap +24 jam (setelah tengah malam)
                            CASE 
                              WHEN breakTimeParam < '08:00:00' 
                                THEN ADDTIME(breakTimeParam, '24:00:00')
                              ELSE breakTimeParam
                            END AS norm_time
                          FROM cataler.breaktime
                        ) AS t
                        WHERE t.norm_time <= (
                          -- Waktu sekarang juga dinormalisasi (kalau sebelum 08:00, +24 jam)
                          CASE 
                            WHEN CURTIME() < '08:00:00' 
                              THEN ADDTIME(CURTIME(), '24:00:00')
                            ELSE CURTIME()
                          END
                        )
                        ORDER BY t.norm_time DESC
                        LIMIT 1;
                        ";
                var totalBreak = await connection.ExecuteScalarAsync<int>(sql);
                _logger.LogInformation($"Total break sampai jam sekarang = {totalBreak} menit");
                return totalBreak;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal mengambil data break time");
                return 0;
            }
        }


        public async Task<Machine_Status_Master?> GetMachineStatusCodeAsync(decimal mcStatus)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);

                string sql = "SELECT * FROM machine_status_master WHERE statusCode = @mcStatus;";

                var statusDetails = await connection.QuerySingleOrDefaultAsync<Machine_Status_Master>(
                    sql, new { mcStatus });

                if (statusDetails != null)
                {
                    _logger.LogInformation($"[MachineStatus] Data ditemukan: StatusCode = {statusDetails.StatusCode}, Label = {statusDetails.StatusLabel}, Color = {statusDetails.StatusColor}");
                }
                else
                {
                    _logger.LogWarning($"[MachineStatus] Tidak ditemukan data untuk StatusCode: {mcStatus}");
                }

                return statusDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[MachineStatus] Gagal mengambil data untuk StatusCode: {mcStatus}");
                return null;
            }
        }

        public async Task<IEnumerable<Machine_Runtime_Summary>> GetMachineRuntimeSummaryAsync(decimal lineNo, DateTime date)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                const string sql = "CALL GetMachineRuntime(@lineNo, @date);";
                var statusDetails = await connection.QueryAsync<Machine_Runtime_Summary>(sql, new { lineNo, date });

                if (statusDetails != null && statusDetails.Any())
                {
                   
                }
                else
                {
                    _logger.LogWarning($"[MachineStatus] Tidak ditemukan data untuk Line: {lineNo}, Date: {date:yyyy-MM-dd}");
                }

                return statusDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[SP] Gagal ambil runtime untuk Line {lineNo}, Date: {date:yyyy-MM-dd}");
                return Enumerable.Empty<Machine_Runtime_Summary>();
            }
        }

        public async Task<IReadOnlyList<Machine_Runtime_Model_Summary>> GetMachineRuntimePerModelAsync(
            int lineNo,
            DateTime modelStart,
            DateTime? modelEnd = null,
            CancellationToken ct = default)
        {
            const string spName = "GetMachineRuntimePerModel_MinutesRaw";

            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync(ct);

                // SP mengembalikan: status (string), totalRuntime (int menit)
                var rows = (await connection.QueryAsync<Machine_Runtime_Model_Summary>(
                    sql: spName,
                    param: new
                    {
                        p_lineNo = lineNo,
                        p_modelStart = modelStart,
                        p_modelEnd = modelEnd
                    },
                    commandType: CommandType.StoredProcedure
                )).ToList();

                if (rows.Count == 0)
                {
                    _logger.LogWarning(
                        "[MachineStatus] Runtime kosong. line={Line}, start={Start:o}, end={End}",
                        lineNo, modelStart, modelEnd?.ToString("o") ?? "NOW()");
                }

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SP] Gagal ambil runtime per-model. line={Line}, start={Start:o}, end={End}",
                    lineNo, modelStart, modelEnd?.ToString("o") ?? "NOW()");
                return Array.Empty<Machine_Runtime_Model_Summary>();
            }
        }


        public async Task<Oa_Target?> GetOaTargetAsync(decimal lineNo)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);

                const string sql = @"SELECT * 
                             FROM oa_target 
                             WHERE lineNo = @lineNo 
                             LIMIT 1;";

                var result = await connection.QueryFirstOrDefaultAsync<Oa_Target>(sql, new { lineNo });

                if (result == null)
                {
                    _logger.LogWarning("[MachineStatus] Tidak ditemukan data untuk Line: {LineNo}", lineNo);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SP] Gagal ambil runtime untuk Line {LineNo}", lineNo);
                return null;
            }
        }
        /// <summary>
        /// Ambil plan berdasarkan aturan hari kerja: 08:00–07:59.
        /// Jika waktu < 08:00, gunakan plan tanggal hari sebelumnya.
        /// </summary>
        public async Task<Production_Plan_Master?> GetPlanAsync(string lineName, string productName, DateTime planDate)
        {
            try
            {
                // Aturan mulai hari: 08:00
                var dayStart = TimeSpan.FromHours(8);

                // Tentukan tanggal efektif untuk query plan
                var effectiveDate = planDate.TimeOfDay < dayStart
                    ? planDate.Date.AddDays(-1)  // sebelum jam 08:00 → pakai kemarin
                    : planDate.Date;             // setelah/tepat 08:00 → pakai hari ini

                if (effectiveDate != planDate.Date)
                {
                    _logger.LogInformation(
                        "[Plan] {Now:HH:mm} < 08:00 → gunakan plan tanggal sebelumnya: {Effective:yyyy-MM-dd}",
                        planDate, effectiveDate);
                }

                using var connection = new MySqlConnection(dbConfig.MysqlConnString);

                const string sql = @"
                        SELECT
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

                var baseCode = productName;

                var plan = await connection.QueryFirstOrDefaultAsync<Production_Plan_Master>(
                    sql,
                    new
                    {
                        lineName = lineName?.Trim(),
                        productName = productName?.Trim(),
                        planDate = effectiveDate
                    });

                if (plan == null)
                {
                    _logger.LogWarning(
                        "[Plan] Not found. Line={Line}, ProductBase={Base}, Date={Date:yyyy-MM-dd}",
                        lineName, baseCode, effectiveDate);
                }

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[Plan] Error. Line={Line}, Product={Prod}, Date={Date:yyyy-MM-dd}",
                    lineName, productName, planDate);
                return null;
            }
        }

        public async Task StatusChangeInsertAndUpdateAsync(decimal lineNo, decimal currentStatus)
        {
            try
            {
                // Hindari tipe mismatch (decimal → int) agar index DB tetap kepakai
                int line = (int)lineNo;
                int status = (int)currentStatus;
                DateTime now = DateTime.Now;

                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();

                using var tx = await connection.BeginTransactionAsync();

                // 1) Tutup status lama yang masih aktif dan BERBEDA dari status baru
                const string updateSql = @"
                    UPDATE machine_run_history
                    SET dateEnd = @dateEnd
                    WHERE lineNo = @line AND dateEnd IS NULL AND status <> @status;";

                await connection.ExecuteAsync(updateSql, new
                {
                    dateEnd = now,
                    line,
                    status
                }, tx);

                // 2) Tambahkan status baru jika BELUM ada yang aktif dengan status yang sama
                const string insertSql = @"
                    INSERT INTO machine_run_history (lineNo, status, dateStart, dateEnd)
                    SELECT @line, @status, @dateStart, NULL
                    FROM DUAL
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM machine_run_history
                        WHERE lineNo = @line AND status = @status AND dateEnd IS NULL
                );";

                int rowsInserted = await connection.ExecuteAsync(insertSql, new
                {
                    line,
                    status,
                    dateStart = now
                }, tx);

                await tx.CommitAsync();

                if (rowsInserted > 0)
                    _logger.LogInformation("Status {Status} untuk Line {LineNo} berhasil disimpan.", status, line);
                else
                    _logger.LogInformation("Status {Status} untuk Line {LineNo} sudah aktif, tidak disimpan ulang.", status, line);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal insert/update status Line {LineNo} dengan status {Status}", lineNo, currentStatus);
            }
        }


        public async Task<int> GetCoCountAsync(int lineNo, DateTime selectedDate)
        {
            using var connection = new MySqlConnection(dbConfig.MysqlConnString);

            // Determine start/end based on 08:00 → 08:00 window
            DateTime start;
            DateTime end;

            if (selectedDate.Hour < 8)
            {
                start = selectedDate.Date.AddDays(-1).AddHours(8); // Yesterday 08:00
                end = selectedDate.Date.AddHours(8);               // Today 08:00
            }
            else
            {
                start = selectedDate.Date.AddHours(8);             // Today 08:00
                end = selectedDate.Date.AddDays(1).AddHours(8);    // Tomorrow 08:00
            }

            const string sql = @"
                    SELECT COUNT(*) 
                    FROM machine_run_history
                    WHERE lineNo = @lineNo
                      AND status = 4
                      AND dateStart >= @startDate
                      AND dateStart < @endDate";

            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                lineNo,
                startDate = start,
                endDate = end
            });
            return count;
        }
        public async Task<int> GetTodayPlanTotalByLineNoAsync(int lineNo)
        {
            try
            {
                var now = DateTime.Now; // langsung pakai local time
                var shiftDate = now.Hour < 8 ? now.Date.AddDays(-1) : now.Date;

                const string sql = @"
                    SELECT COALESCE(SUM(ppm.planQty), 0)
                    FROM production_plan_master AS ppm
                    INNER JOIN line_master AS lm ON lm.id = ppm.lineMasterId
                    WHERE ppm.planDate = @planDate
                      AND lm.lineNo = @lineNo";

                using var conn = new MySqlConnection(dbConfig.MysqlConnString);
                await conn.OpenAsync();

                var total = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    planDate = shiftDate, // kolom DATE akan cocok dengan DateTime.Date ini
                    lineNo
                });

                _logger.LogInformation("[Plan] Total plan shift-date {ShiftDate} = {Total} (now={Now:yyyy-MM-dd HH:mm}) lineNo={LineNo}",
                    shiftDate.ToString("yyyy-MM-dd"), total, now, lineNo);

                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Plan] Gagal hitung total plan (lineNo={LineNo})", lineNo);
                return 0;
            }
        }

        public async Task<int?> AutoRegisterPlan(
             string lineName,
             string productName,
             DateOnly planDate,
             int planQty,
             int defaultWorkStatusId = 1)
        {
            try
            {
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();
                using var tx = await connection.BeginTransactionAsync();

                // 🔧 format tanggal "YYYY-MM-DD" (sesuai permintaan)
                var planDateStr = planDate.ToString("yyyy-MM-dd");

                // 1) line_master
                var line = await connection.QuerySingleOrDefaultAsync<(int Id, int LineNo)>(
                    @"SELECT id, lineNo
                        FROM line_master
                        WHERE lineName = @lineName
                        LIMIT 1",
                    new { lineName }, tx);

                if (line.Id == 0)
                {
                    _logger.LogWarning("[Plan] line_master tidak ditemukan untuk lineName={LineName}", lineName);
                    return null;
                }

                // 2) product_master
                int productMasterId = await connection.ExecuteScalarAsync<int?>(
                    @"SELECT id
                      FROM product_master
                      WHERE lineNo = @lineNo
                        AND productName = @productName
                        AND IFNULL(IsDeleted,0)=0
                      LIMIT 1",
                    new { line.LineNo, productName }, tx) ?? 0;

                if (productMasterId == 0)
                {
                    productMasterId = await connection.ExecuteScalarAsync<int>(
                        @"INSERT INTO product_master (lineNo, productName, IsDeleted)
                  VALUES (@lineNo, @productName, 0);
                  SELECT LAST_INSERT_ID();",
                        new { line.LineNo, productName }, tx);

                    _logger.LogInformation("[Plan] product_master baru dibuat: {ProductName} (id={ProductId})", productName, productMasterId);
                }

                // 3) Cek existing (pakai string tanggal)
                var existingPlanId = await connection.ExecuteScalarAsync<int?>(
                    @"SELECT id
                      FROM production_plan_master
                      WHERE planDate = @planDate   -- DATE kolom, tapi MySQL fine dengan 'YYYY-MM-DD'
                        AND lineMasterId = @lineMasterId
                        AND productMasterId = @productMasterId
                      LIMIT 1",
                    new { planDate = planDateStr, lineMasterId = line.Id, productMasterId }, tx);

                if (existingPlanId.HasValue)
                {
                    await tx.CommitAsync();
                    _logger.LogInformation("[Plan] Sudah ada plan id={PlanId} untuk {LineName}-{ProductName} {PlanDate}",
                        existingPlanId.Value, lineName, productName, planDateStr);
                    return existingPlanId.Value;
                }

                // 4) Insert baru: set fileName = 'Automatic', (opsional) dateFile = planDate
                var newPlanId = await connection.ExecuteScalarAsync<int>(
                        @"INSERT INTO production_plan_master
                        (lineMasterId, productMasterId, planDate, planQty, work_status_masterId, fileName, dateFile, createdAt)
                    VALUES
                        (@lineMasterId, @productMasterId, @planDate, @planQty, @statusId, @fileName, @dateFile, NOW());
                    SELECT LAST_INSERT_ID();
                        ",
                    new
                    {
                        lineMasterId = line.Id,
                        productMasterId,
                        planDate = planDateStr,          // '2025-09-19'
                        planQty,
                        statusId = defaultWorkStatusId,
                        fileName = "Automatic",          // ← sesuai permintaan
                        dateFile = planDateStr,
                    }, tx);

                await tx.CommitAsync();

                _logger.LogInformation("[Plan] Plan baru dibuat id={PlanId} untuk {LineName}-{ProductName} {PlanDate} qty={Qty}",
                    newPlanId, lineName, productName, planDateStr, planQty);

                return newPlanId;
            }
            catch (MySqlException sqlEx) when (sqlEx.Number == 1062) // duplicate key
            {
                // Handle race: ambil id existing (pakai string tanggal juga)
                var planDateStr = planDate.ToString("yyyy-MM-dd");

                using var connection2 = new MySqlConnection(dbConfig.MysqlConnString);
                var planId = await connection2.ExecuteScalarAsync<int?>(
                    @"SELECT ppm.id
              FROM production_plan_master ppm
              JOIN line_master lm ON lm.id = ppm.lineMasterId
              JOIN product_master pm ON pm.id = ppm.productMasterId
              WHERE ppm.planDate = @planDate
                AND lm.lineName = @lineName
                AND pm.productName = @productName
                AND pm.lineNo = lm.lineNo
              LIMIT 1",
                    new { planDate = planDateStr, lineName, productName });

                _logger.LogWarning(sqlEx, "[Plan] Duplikat terdeteksi, kembalikan id yang sudah ada: {PlanId}", planId);
                return planId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Plan] Gagal AutoRegisterPlan {LineName}-{ProductName} {PlanDate} qty={Qty}",
                    lineName, productName, planDate.ToString("yyyy-MM-dd"), planQty);
                return null;
            }
        }
        public async Task<string?> GetLineNameByLineNoAsync(int lineNo)
        {
            try
            {
                const string sql = @"
            SELECT lineName
            FROM line_master
            WHERE lineNo = @lineNo
            LIMIT 1;";

                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync();

                var lineName = await connection.QuerySingleOrDefaultAsync<string>(
                    sql,
                    new { lineNo }
                );

                if (lineName == null)
                {
                    _logger.LogWarning("[LineMaster] Tidak ditemukan LineName untuk LineNo={LineNo}", lineNo);
                }
                else
                {
                    _logger.LogInformation("[LineMaster] LineNo={LineNo}, LineName={LineName}", lineNo, lineName);
                }

                return lineName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LineMaster] Gagal mengambil LineName untuk LineNo={LineNo}", lineNo);
                return null;
            }
        }


        /// <summary>
        /// Dapatkan shift aktif pada waktu "now" (default DateTime.Now) untuk scheduleType tertentu.
        /// - scheduleType: 'NORMAL' atau 'RAMADAN'
        /// - now: optional override waktu (mis. untuk simulasi/testing)
        /// </summary>
        public async Task<ShiftDto?> GetShiftNowAsync(
            string scheduleType = "NORMAL",
            DateTime? now = null,
            CancellationToken ct = default)
        {
            try
            {
                const string sql = @"
                SELECT 
                    sm.code           AS Code,
                    sm.name           AS Name,
                    ss.scheduleType   AS ScheduleType,
                    ss.startTime      AS StartTime,
                    ss.endTime        AS EndTime,
                    ss.crossesMidnight AS CrossesMidnight
                FROM shift_schedule ss
                JOIN shift_master   sm ON sm.id = ss.shiftId
                WHERE ss.isActive = 1
                  AND ss.scheduleType = @scheduleType
                  AND (
                        (ss.crossesMidnight = 0 AND @nowTime >= ss.startTime AND @nowTime < ss.endTime)
                     OR (ss.crossesMidnight = 1 AND (@nowTime >= ss.startTime OR  @nowTime < ss.endTime))
                  )
                LIMIT 1";

                var nowTime = (now ?? DateTime.Now).TimeOfDay; // TimeSpan -> otomatis ter-map ke TIME
                using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                await connection.OpenAsync(ct);

                var result = await connection.QuerySingleOrDefaultAsync<ShiftDto>(
                    new CommandDefinition(
                        sql,
                        new { scheduleType, nowTime },
                        cancellationToken: ct));

                if (result is null)
                {
                    _logger.LogWarning("[Shift] Shift aktif tidak ditemukan. scheduleType={ScheduleType}, nowTime={NowTime}",
                        scheduleType, nowTime);
                    return null;
                }

                _logger.LogInformation("[Shift] Active={Code} ({Name}) {Start}-{End} scheduleType={ScheduleType} crossesMidnight={Cross}",
                    result.Code, result.Name, result.StartTime, result.EndTime, result.ScheduleType, result.CrossesMidnight);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Shift] Gagal mendapatkan shift aktif. scheduleType={ScheduleType}", scheduleType);
                return null;
            }
        }

        /// <summary>
        /// Versi ringkas: hanya mengembalikan code shift (misal 'S1', 'LS2').
        /// </summary>
        public async Task<string?> GetShiftCodeNowAsync(
            string scheduleType = "NORMAL",
            DateTime? now = null,
            CancellationToken ct = default)
        {
            var dto = await GetShiftNowAsync(scheduleType, now, ct);
            return dto?.Code;
        }

        /// <summary>
        /// Mengembalikan status: INSIDE (di dalam shift) atau GAP (jeda S1/S2),
        /// plus prevEndDateTime & nextStartDateTime.
        /// </summary>
        public async Task<Shift_Inside_Gap_Result?> GetShiftInsideOrGapAsync(
        string scheduleType = "NORMAL",
        DateTime? nowTs = null,
        CancellationToken ct = default)
        {
            const string sql = @"
                WITH
                params AS (
                  SELECT @scheduleType AS scheduleType, @nowTs AS nowTs
                ),
                base AS (
                  SELECT sm.code, ss.scheduleType, ss.startTime, ss.endTime, ss.crossesMidnight
                  FROM shift_schedule ss
                  JOIN shift_master   sm ON sm.id = ss.shiftId
                  WHERE ss.isActive = 1
                    AND ss.scheduleType = (SELECT scheduleType FROM params)
                    AND sm.code IN ('S1','S2')
                ),
                anchors AS (
                  SELECT code, scheduleType, startTime, endTime, crossesMidnight, DATE((SELECT nowTs FROM params)) AS anchorDate
                  FROM base
                  UNION ALL
                  SELECT code, scheduleType, startTime, endTime, crossesMidnight, DATE((SELECT nowTs FROM params)) - INTERVAL 1 DAY AS anchorDate
                  FROM base
                ),
                intervals AS (
                  SELECT
                    code,
                    scheduleType,
                    TIMESTAMP(anchorDate, startTime) AS startDateTime,
                    TIMESTAMP(anchorDate, endTime)
                      + INTERVAL (CASE WHEN crossesMidnight = 1 AND endTime <= startTime THEN 1 ELSE 0 END) DAY AS endDateTime
                  FROM anchors
                ),
                filtered AS (
                  SELECT *
                  FROM intervals
                  WHERE endDateTime   > (SELECT nowTs - INTERVAL 24 HOUR FROM params)
                    AND startDateTime < (SELECT nowTs + INTERVAL 24 HOUR FROM params)
                ),
                inside AS (
                  SELECT
                    'INSIDE' AS mode,
                    f.code   AS currentCode,
                    NULL     AS prevCode,
                    NULL     AS nextCode,
                    NULL     AS gapSeconds,
                    NULL     AS gapMinutes,
                    f.startDateTime,
                    f.endDateTime,
                    TIMESTAMPDIFF(SECOND, (SELECT nowTs FROM params), f.endDateTime) AS secondsToShiftEnd,
                    TIMESTAMPDIFF(MINUTE, (SELECT nowTs FROM params), f.endDateTime) AS minutesToShiftEnd
                  FROM filtered f
                  WHERE (SELECT nowTs FROM params) >= f.startDateTime
                    AND (SELECT nowTs FROM params) <  f.endDateTime
                  ORDER BY f.endDateTime
                  LIMIT 1
                ),
                next_up AS (
                  SELECT code, startDateTime
                  FROM filtered
                  WHERE startDateTime > (SELECT nowTs FROM params)
                  ORDER BY startDateTime
                  LIMIT 1
                ),
                prev_down AS (
                  SELECT code, endDateTime
                  FROM filtered
                  WHERE endDateTime <= (SELECT nowTs FROM params)
                  ORDER BY endDateTime DESC
                  LIMIT 1
                ),
                gap AS (
                  SELECT
                    'GAP' AS mode,
                    NULL  AS currentCode,
                    (SELECT code           FROM prev_down) AS prevCode,
                    (SELECT code           FROM next_up )  AS nextCode,
                    TIMESTAMPDIFF(SECOND, (SELECT nowTs FROM params), (SELECT startDateTime FROM next_up)) AS gapSeconds,
                    TIMESTAMPDIFF(MINUTE, (SELECT nowTs FROM params), (SELECT startDateTime FROM next_up)) AS gapMinutes,
                    NULL AS startDateTime,
                    NULL AS endDateTime,
                    NULL AS secondsToShiftEnd,
                    NULL AS minutesToShiftEnd
                  WHERE (SELECT COUNT(*) FROM inside) = 0
                ),
                final_union AS (
                  SELECT * FROM inside
                  UNION ALL
                  SELECT * FROM gap
                )
                SELECT
                  fu.mode,
                  fu.currentCode,
                  fu.prevCode,
                  pd.endDateTime   AS prevEndDateTime,
                  fu.nextCode,
                  nu.startDateTime AS nextStartDateTime,
                  fu.gapSeconds,
                  fu.gapMinutes,
                  fu.startDateTime,
                  fu.endDateTime,
                  fu.secondsToShiftEnd,
                  fu.minutesToShiftEnd
                FROM final_union fu
                LEFT JOIN prev_down pd ON 1=1
                LEFT JOIN next_up  nu ON 1=1;";

            var p = new
            {
                scheduleType,
                nowTs = nowTs ?? DateTime.Now
            };

            using var conn = new MySqlConnection(dbConfig.MysqlConnString);
            await conn.OpenAsync(ct);

            // QueryFirstOrDefaultAsync karena hasilnya 0–1 baris
            var result = await conn.QueryFirstOrDefaultAsync<Shift_Inside_Gap_Result>(
                new CommandDefinition(sql, p, cancellationToken: ct));

            return result;
        }


        public async Task<int> GetCOCountAsync(int lineNo)
        {
            const string sql = @"
                 SELECT GREATEST(COUNT(*) - 1, 0) AS COCount
                 FROM production_history ph
                 JOIN production_plan_master ppm ON ppm.id = ph.productionPlanId
                 JOIN line_master lm ON lm.id = ppm.lineMasterId
                 WHERE lm.lineNo = @lineNo
                   AND COALESCE(ph.ActualQty, 0) <> 0
                   AND ph.`timestamp` >= (
                         CASE
                           WHEN TIME(NOW()) < '08:00:00'
                             THEN CONCAT(CURDATE() - INTERVAL 1 DAY, ' 08:00:01')
                           ELSE CONCAT(CURDATE(), ' 08:00:01')
                         END
                       )
                   AND ph.`timestamp` < (
                         CASE
                           WHEN TIME(NOW()) < '08:00:00'
                             THEN CONCAT(CURDATE(), ' 08:00:01')
                           ELSE CONCAT(CURDATE() + INTERVAL 1 DAY, ' 08:00:01')
                         END
                       )";
            try
            {
                await using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                var count = await connection.ExecuteScalarAsync<int>(sql, new { lineNo });

                _logger.LogInformation(
                    "[CO] Count window dynamic (08:01 prev/next) = {Count} for lineNo={LineNo}",
                    count, lineNo);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CO] Gagal menghitung COCount untuk lineNo={LineNo}", lineNo);
                return 0;
            }
        }

        /*public async Task<int> GetCOCountAsync(int lineNo)
        {
            const string sql = @"
                SELECT GREATEST(COUNT(*) - 1, 0) AS COCount
                FROM production_history ph
                JOIN production_plan_master ppm ON ppm.id = ph.productionPlanId
                JOIN line_master lm ON lm.id = ppm.lineMasterId
                WHERE lm.lineNo = @lineNo
                  AND COALESCE(ph.ActualQty, 0) <> 0
                  AND ph.`timestamp` >= (
                        CASE
                          WHEN TIME(NOW()) < '08:00:00'
                            THEN CONCAT(CURDATE() - INTERVAL 1 DAY, ' 08:00:00')
                          ELSE CONCAT(CURDATE(), ' 08:00:00')
                        END
                      )
                  AND ph.`timestamp` < (
                        CASE
                          WHEN TIME(NOW()) < '08:00:00'
                            THEN CONCAT(CURDATE(), ' 08:00:00')
                          ELSE CONCAT(CURDATE() + INTERVAL 1 DAY, ' 08:00:00')
                        END
                      )";
            try
            {
                await using var connection = new MySqlConnection(dbConfig.MysqlConnString);
                var count = await connection.ExecuteScalarAsync<int>(sql, new { lineNo });

                _logger.LogInformation(
                    "[CO] Count window dynamic (08:00 prev/next) = {Count} for lineNo={LineNo}",
                    count, lineNo);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CO] Gagal menghitung COCount untuk lineNo={LineNo}", lineNo);
                return 0;
            
        }*/
    }
}
