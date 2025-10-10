using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.MasterData;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;
using Web.API.Persistence.Services;

namespace Web.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataService _svc;
        private readonly IWebHostEnvironment _env;
        public MasterDataController(IMasterDataService masterDataService, IWebHostEnvironment env)
        {
            _svc = masterDataService;
            _env = env;
        }

        /// <summary>
        /// Get Card No Master
        /// </summary>
        /// <param name="page"></param>
        /// <param name="limit"></param>
        /// <param name="lineNo"></param>
        /// <param name="productName"></param>
        /// <param name="materialName"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CardNoMaster>>>> GetCardNoMasters(
        int page = 1,
        int limit = 10,
        int? id = null,
        string? lineNo = null,
        string? productName = null, 
        string? materialName = null)
        {
            var result = await _svc.GetCardNoMastersAsync(page, limit,id, lineNo, productName,materialName);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        // GET: api/CardNoMaster/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var res = await _svc.GetCardNoMasterByIdAsync(id);

            if (!res.Success || res.Data is null)
                return NotFound(new { message = res.Message ?? $"Data dengan Id {id} tidak ditemukan." });

            return Ok(res); // ApiResponse<CardNoMaster?>
        }


        [HttpGet("product-masters")]
        public async Task<IActionResult> GetProductMasters([FromQuery]ProductMasterRequest data)
        {
            var result = await _svc.GetProductMastersAsync(data.LineNo);
            return Ok(result);
        }

        // NEW: hanya kolom Id, LineNo, CardNo, ProductName, filter by LineNo
        [HttpGet("sub-product-master-list")]
        [ProducesResponseType(typeof(ApiResponse<List<GetSubProductMasterListDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<GetSubProductMasterListDto>>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<List<GetSubProductMasterListDto>>>> GetProductNamesByLineNo(
            [FromQuery] string lineNo)
        {
            if (string.IsNullOrWhiteSpace(lineNo))
                return BadRequest(new ApiResponse<List<GetSubProductMasterListDto>>
                {
                    Success = false,
                    Message = "Parameter 'lineNo' wajib diisi.",
                    Data = new()
                });

            var result = await _svc.GetProductNamesByLineNoAsync(lineNo);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("by-cardno")]
        public async Task<ActionResult<ApiResponse<CardNoMaster?>>> GetByCardNo([FromQuery] GetSpesificMasterByCardNoDto request)
        {
            if (request.CardNo <= 0 || request.LineNo <= 0)
                return BadRequest(new ApiResponse<CardNoMaster?>
                {
                    Success = false,
                    Message = "Parameter 'cardNo' dan 'lineNo' wajib diisi dan harus > 0.",
                    Data = null
                });

            var result = await _svc.GetByCardNoAsync(request.CardNo, request.LineNo);
            return result.Success ? Ok(result) : NotFound(result);
        }



        [HttpPost("CreateCardNoMaster")]
        public async Task<IActionResult> Insert([FromBody] CardNoMaster dto)
        {
            if (dto == null)
                return BadRequest("Data tidak boleh kosong.");

            var (success, message) = await _svc.InsertCardNoMasterAsync(dto);

            if (!success)
                return StatusCode(500, $"Gagal menyimpan data: {message}");

            return Ok("Data berhasil disimpan.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CardNoMasterUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Data update tidak boleh kosong.");

            var (Success, Message) = await _svc.UpdateAsync(id, dto);

            if (!Success)
                return NotFound(Message);

            return NoContent(); // 204 No Content jika sukses update
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var (Success, Message) = await _svc.SoftDeleteAsync(id);

            if (!Success)
                return NotFound(Message);

            return NoContent(); // 204 kalau berhasil soft delete
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> ImportExcel([FromForm] UploadExcel request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File tidak ditemukan atau kosong.");

            var (Success, Message) = await _svc.ImportExcelAsync(request.File);

            if (Success)
                return Ok(new { message = "Import berhasil." });
            else
                return BadRequest(new { message = Message ?? "Import gagal." });
        }

        // GET: api/card-no-master/export
        [HttpGet("export")]
        public async Task<IActionResult> Export(CancellationToken ct)
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Template", "CardMaster.xlsx");

            var (ok, msg, bytes, fileName, _) = await _svc.ExportCardNoMastersAsync(templatePath, ct);

            if (!ok || bytes is null)
                return BadRequest(new { message = msg ?? "Export failed" });

            const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(bytes, contentType, fileName ?? "CardMaster.xlsx");
        }
    }
}
