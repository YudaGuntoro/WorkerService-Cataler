using Microsoft.AspNetCore.Mvc;
using Web.API.Persistence.Services;
using Web.API.Mappings.Response;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.ProductionPlan;
using Web.API.Mappings.Request;
using MiniExcelLibs;

namespace Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionPlanController : ControllerBase
    {
        private readonly IProductionPlanService _svc;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IWebHostEnvironment _env;
        public ProductionPlanController(IWebHostEnvironment env, IProductionPlanService productionPlanService)
        {
            _env = env;
            _svc = productionPlanService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync(
        [FromQuery] DateOnly? date = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] int? lineNo = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
        {
            var result = await _svc.GetAllAsync(date, startDate, endDate, lineNo, page, limit);
            return Ok(result);
        }

        [HttpGet("product-master")]
        public async Task<ActionResult<List<ProductMaster>>> GetAllProductMaster()
        {
            var products = await _svc.GetAllProductMaster();
            return Ok(products);
        }

        [HttpGet("Line")]
        public async Task<ActionResult<List<ProductMaster>>> GetAllLineMaster()
        {
            var products = await _svc.GetAllLineMaster();
            return Ok(products);
        }

        [HttpPost("CreateProductionPlan")]
        public async Task<IActionResult> Create([FromBody] ProductionPlanCreateDto dto)
        {
            var (success, message) = await _svc.CreateAsync(dto);
            if (!success)
                return BadRequest(new { success, message });

            return Ok(new { success });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductionPlanMaster dto)
        {
            var result = await _svc.UpdateAsync(id, dto);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = "Production plan updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _svc.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = "Production plan deleted successfully." });
        }

        [HttpPost("uploadplan")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadData([FromForm] UploadExcel request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file was uploaded.");

            var result = await _svc.ImportExcelAsync(request.File);

            if (!result.Success)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                Message = "Upload and data import successful.",
                FileName = request.File.FileName
            });
        }

        [HttpGet("production-plan/export")]
        public async Task<IActionResult> ExportProductionPlan(
                [FromQuery] DateOnly? date,
                [FromQuery] DateOnly? startDate,
                [FromQuery] DateOnly? endDate,
                [FromQuery] int? lineNo,
                CancellationToken ct)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Template", "ProductionPlanData.xlsx");

            var (ok, msg, bytes, fileName) = await _svc.ExportProductionPlansAsync(
                templatePath, date, startDate, endDate, lineNo, ct);

            if (!ok || bytes is null)
                return BadRequest(new { message = msg ?? "Export failed" });

            const string ctExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, ctExcel, fileName);
        }



        /// <summary>
        /// Ambil semua target (0–23) untuk line tertentu, terurut by operationHour.
        /// </summary>
        /// <param name="lineId">ID line/master.</param>
        // GET: /api/ProductionPlan/Targets/5
        [HttpGet("Targets/{lineId:int}")]
        public async Task<ActionResult<List<ProductionCountMaster>>> GetAllTargetsByLine(
            int lineId, CancellationToken ct = default)
        {
            var (ok, data, msg) = await _svc.GetAllTargetsByLineAsync(lineId, ct);
            if (!ok) return NotFound(msg);
            return Ok(data);
        }
    }
}
