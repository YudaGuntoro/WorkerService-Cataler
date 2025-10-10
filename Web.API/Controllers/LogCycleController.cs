using Microsoft.AspNetCore.Mvc;
using Web.API.Persistence.Services;
using Web.API.Models.Response;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Mappings.Response;

namespace Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogCycleController : ControllerBase
    {
        private readonly ILogCycleService _svc;
        public LogCycleController(ILogCycleService logCycleService)
        {
            _svc = logCycleService;
        }
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<GetHistoryListCycleTimeDto>>>> GetAllAsync(
                        [FromQuery] int page = 1,
                        [FromQuery] int limit = 10,
                        [FromQuery] int? lineNo = null,
                        [FromQuery] DateTime? date = null,
                        [FromQuery] DateTime? startDate = null,
                        [FromQuery] DateTime? endDate = null)
        {
            var response = await _svc.GetAllAsync(page, limit, lineNo, date, startDate, endDate);

            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        // GET: api/alarm-log/export-cycletime?page=1&limit=50&lineNo=5041&startDate=2025-08-01&endDate=2025-08-21
        [HttpGet("export-cycletime")]
        public async Task<IActionResult> ExportCycleTime(
            [FromQuery] int page = 1,
            [FromQuery] int? limit = null,          // default 100 kalau tidak diisi
            [FromQuery] int? lineNo = null,
            [FromQuery] DateTime? date = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var (ok, msg, bytes, fileName) = await _svc.ExportCycleTimeAsync(
                page, limit, lineNo, date, startDate, endDate);

            if (!ok || bytes is null)
                return BadRequest(new { message = msg ?? "Export failed" });

            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, fileName ?? "LogCycleTime.xlsx");
        }
    }
}
