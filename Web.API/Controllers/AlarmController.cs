using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Mappings.Response;
using Web.API.Persistence.Services;

namespace Web.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlarmController : ControllerBase
    {
        private readonly ILogAlarmService _svc;
        private readonly IWebHostEnvironment _env;
        public AlarmController(ILogAlarmService svc, IWebHostEnvironment env)
        {
            _svc = svc;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
       [FromQuery] int page = 1,
       [FromQuery] int limit = 10,
       [FromQuery] int? lineNo = null,
       [FromQuery] DateTime? startDate = null,
       [FromQuery] DateTime? endDate = null)
        {
            var result = await _svc.GetAllAsync(page, limit, lineNo, startDate, endDate);
            return Ok(result);
        }

        // GET: api/alarm-log/export-failure?page=1&limit=50&lineNo=5041&startDate=2025-08-20&endDate=2025-08-21&keyword=valve
        [HttpGet("export-failure")]
        public async Task<IActionResult> ExportFailure(
            [FromQuery] int page = 1,
            [FromQuery] int? limit = null,          // default 100 kalau null
            [FromQuery] int? lineNo = null,
            [FromQuery] DateTime? date = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? keyword = null)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Template", "FailureDetails.xlsx");

            var (ok, msg, bytes, fileName) = await _svc.ExportFailureDetailsAsync(
                templatePath, page, limit, lineNo, date, startDate, endDate, keyword);

            if (!ok || bytes is null) return BadRequest(new { message = msg ?? "Export failed" });

            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, fileName ?? "FailureDetails.xlsx");
        }
    }
}