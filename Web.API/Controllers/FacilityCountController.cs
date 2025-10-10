using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.API.Mappings.DTOs.FacilityCount;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;
using Web.API.Persistence.Services;
using Web.API.Persistence.Shared;

namespace Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacilityCountController : ControllerBase
    {
        private readonly IFacilityCountService _svc;
        private readonly IWebHostEnvironment _env;
        public FacilityCountController(IFacilityCountService svc, IWebHostEnvironment env)
        {
            _svc = svc;
            _env = env;
        }

        private const string M1 = "m1";
        private static readonly string TopicM1 = "Toho-Tech/Machine 1";

        private const string M2 = "m2";
        private static readonly string TopicM2 = "Toho-Tech/Machine 2";


        [AllowAnonymous]
        // ===== Live & Snapshot (Machine 1) =====
        [HttpGet("machine1/live")]
        [ProducesResponseType(typeof(ApiResponse<List<FacilityCountRealtimeDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LiveM1(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var res = await _svc.GetLiveAsync(M1, TopicM1, 1, page, limit, startDate, endDate, HttpContext.RequestAborted);
            return res.Success ? Ok(res) : StatusCode(500, res);
        }

        [AllowAnonymous]
        [HttpPost("machine1/snapshot")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SnapshotM1()
        {
            var res = await _svc.CreateSnapshotAsync(M1, TopicM1, 1, HttpContext.RequestAborted);
            return res.Success ? Ok(res) : StatusCode(500, res);
        }

        [AllowAnonymous]
        // ===== Live & Snapshot (Machine 2) =====
        [HttpGet("machine2/live")]
        [ProducesResponseType(typeof(ApiResponse<List<FacilityCountRealtimeDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LiveM2(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var res = await _svc.GetLiveAsync(M2, TopicM2, 2, page, limit, startDate, endDate, HttpContext.RequestAborted);
            return res.Success ? Ok(res) : StatusCode(500, res);
        }

        [AllowAnonymous]
        [HttpPost("machine2/snapshot")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SnapshotM2()
        {
            var res = await _svc.CreateSnapshotAsync(M2, TopicM2, 2, HttpContext.RequestAborted);
            return res.Success ? Ok(res) : StatusCode(500, res);
        }

        // ===== Update config by Id (Device rename, Category, LimitValue) =====
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateConfigById(
            int id,
            [FromBody] UpdateFacilityCount request)
        {
            var res = await _svc.UpdateDeviceConfigByIdAsync(
                id,
                newDevice: request.Device,
                category: request.Category,
                limitValue: request.LimitValue,
                ct: HttpContext.RequestAborted);

            return res.Success ? Ok(res) : StatusCode(500, res);
        }

        // GET: api/facility-count/export?lineNo=5041   (tanpa query -> ALL)
        [HttpGet("export")]
        public async Task<IActionResult> Export([FromQuery] int? lineNo, CancellationToken ct)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Template", "FacilityCount.xlsx");

            var (ok, msg, bytes, fileName) = await _svc.ExportFacilityCountAsync(templatePath, lineNo, ct);

            if (!ok || bytes is null)
                return BadRequest(new { message = msg ?? "Export failed" });

            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, fileName ?? "FacilityCount.xlsx");
        }
    }
}