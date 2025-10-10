using Microsoft.AspNetCore.Mvc;
using Web.API.Mappings.Request;
using Web.API.Persistence.Services;

namespace Web.API.Controllers
{
    [ApiController]
    [Route("api/coat-width-control")]
    public class CoatWidthControlController : ControllerBase
    {
        private readonly ICoatWidthControlService _svc;
        public CoatWidthControlController(ICoatWidthControlService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] int? lineMasterId = null,
            [FromQuery] string? subProductName = null,
            [FromQuery] int? coatingNo = null,               // ⬅️ baru
            [FromQuery] DateTime? recordDate = null,
            [FromQuery] DateTime? startRecordDate = null,
            [FromQuery] DateTime? endRecordDate = null)
        {
            var resp = await _svc.GetAllAsync(page, limit,lineMasterId, subProductName, coatingNo, recordDate, startRecordDate, endRecordDate);
            return resp.Success ? Ok(resp) : BadRequest(resp);
        }

        /// <summary>
        /// Get single CoatWidthControl by Id.
        /// </summary>
        [HttpGet("get-by-id/{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            var res = await _svc.GetByIdAsync(id, ct);

            if (!res.Success || res.Data is null)
                return NotFound(new { message = res.Message ?? $"Data dengan Id '{id}' tidak ditemukan." });

            return Ok(res);
        }

        [HttpGet("by-date-range")]
        public async Task<IActionResult> GetByDateRange(
            [FromQuery] DateTime? recordDate = null,
            [FromQuery] DateTime? startRecordDate = null,
            [FromQuery] DateTime? endRecordDate = null,
            [FromQuery] int? lineMasterId = null,
            [FromQuery] string? subProductName = null,
            [FromQuery] int? coatingNo = null)               // ⬅️ baru
        {
            if (!recordDate.HasValue && !startRecordDate.HasValue && !endRecordDate.HasValue)
                return BadRequest(new { message = "Harus mengirim minimal salah satu: recordDate atau startRecordDate/endRecordDate." });

            var resp = await _svc.GetByDateRangeAsync(recordDate, startRecordDate, endRecordDate, lineMasterId, subProductName, coatingNo);
            return resp.Success ? Ok(resp) : BadRequest(resp);
        }

        /// <summary>
        /// Ambil detail coat width control berdasarkan ID.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _svc.GetByIdAsync(id);
            if (!res.Success) return BadRequest(res);
            return res.Data is null ? NotFound(res) : Ok(res);
        }

        /// <summary>
        /// Buat data coat width control baru.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CoatWidthControlCreate request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ok, msg, id) = await _svc.CreateAsync(request);
            if (!ok) return BadRequest(new { message = msg });

            return CreatedAtAction(nameof(GetById), new { id }, new { id, message = msg });
        }

        /// <summary>
        /// Update data coat width control berdasarkan ID.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CoatWidthControlCreate request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ok, msg, updatedId) = await _svc.UpdateAsync(id, request);
            if (!ok) return NotFound(new { message = msg });

            return Ok(new { id = updatedId, message = msg });
        }

        /// <summary>
        /// Hapus data coat width control berdasarkan ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var (ok, msg) = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound(new { message = msg });
        }
    }
}
