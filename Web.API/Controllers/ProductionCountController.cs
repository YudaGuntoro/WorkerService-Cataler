using Microsoft.AspNetCore.Mvc;
using Web.API.Mappings.DTOs.ProductionCount;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;
using Web.API.Persistence.Services;

namespace Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionCountController : ControllerBase
    {
        private readonly IProductionCountService _svc;

        public ProductionCountController(IProductionCountService svc)
        {
            _svc = svc;
        }

        // GET: api/ProductionCount/history
        // Tanpa pagination & tanpa date range (hanya lineNo + date opsional)
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<GetHistoryListProductionCountDto>>>> GetAllAsync(
            [FromQuery] int? lineNo = null)
        {
            var response = await _svc.GetAllAsync(lineNo);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        // ==== MASTER: GET list (tetap pakai pagination) ====
        [HttpGet("master")]
        public async Task<ActionResult<ApiResponse<List<ProductionCountMasterDto>>>> GetMasterAsync(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] int? lineNo = null,
            [FromQuery] int? lineMasterId = null)
        {
            var resp = await _svc.GetMasterAsync(page, limit, lineNo, lineMasterId);
            if (!resp.Success) return BadRequest(resp);
            return Ok(resp);
        }

        // ==== MASTER: PUT by Id ====
        [HttpPut("master/{id:int}")]
        public async Task<IActionResult> UpdateMasterAsync(int id, [FromBody] ProductionCountMasterUpdateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ok, msg) = await _svc.UpdateMasterAsync(id, request);
            return ok ? NoContent() : NotFound(new { message = msg });
        }
    }
}