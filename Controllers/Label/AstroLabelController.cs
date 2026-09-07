using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SVN_Tools.Models.Label;
using SVN_Tools.Models.Label.DTOs;
using SVN_Tools.Models.Utils;
using SVN_Tools.Services.Utils;

namespace SVN_Tools.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AstroLabelController : ControllerBase
    {
        private readonly IAstroLabelDataService _astroLabelDataService;
        private readonly WalterLogService _walterLogService;

        public AstroLabelController(WalterLogService walterLogService, IAstroLabelDataService astroLabelDataService)
        {
            _astroLabelDataService = astroLabelDataService;
            _walterLogService = walterLogService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AstroLabelDataDto astroLabelDataDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var astroLabelData = new AstroLabelData
            {
                Date = astroLabelDataDto.Date,
                PackageID = astroLabelDataDto.PackageID,
                Serial = astroLabelDataDto.Serial,
                ScanDate = astroLabelDataDto.ScanDate,
                PalletID = astroLabelDataDto.PalletID,
                EmployeeID = astroLabelDataDto.EmployeeID,
                CountSerial = astroLabelDataDto.CountSerial
                // isDeleted có thể mặc định là 0
            };

            var createdData = await _astroLabelDataService.CreateAsync(astroLabelData);

            return CreatedAtAction(nameof(GetByPalletId), new { PalletID = createdData.PalletID }, createdData);
        }

        [HttpGet("palletID/{palletID}")]
        public async Task<IActionResult> GetByPalletId(string palletID)
        {
            var data = await _astroLabelDataService.GetByPalletIdAsync(palletID);
            if (data == null)
            {
                return NotFound();
            }
            return Ok(data);
        }


        [HttpGet("GetPackageByPrefix/{prefix}")]
        public async Task<ActionResult<List<AstroLabelData>>> GetPackageListByPrefix(string prefix)
        {
            // if (string.IsNullOrEmpty(prefix) || prefix.Length != 8)
            // {
            //     return BadRequest("Prefix không hợp lệ. Vui lòng cung cấp prefix có dạng 'YYYYMMDD'.");
            // }

            var results = await _astroLabelDataService.GetPackageListByPrefix(prefix);

            if (results == null || !results.Any())
            {
                return NotFound($"Không tìm thấy dữ liệu với prefix '{prefix}'.");
            }
            var maxPackage = results.OrderByDescending(d => d.PackageID).FirstOrDefault();

            return Ok(maxPackage.PackageID);
        }


        [HttpGet("checkserial/{prefix}/{serial}")]
        public async Task<ActionResult<bool>> CheckSerialExist(string prefix, string serial)
        {
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(serial))
            {
                return BadRequest("Vui lòng cung cấp cả prefix ngày và serial để kiểm tra.");
            }

            bool isExist = await _astroLabelDataService.checkSerialExist(prefix, serial);

            return Ok(isExist);
        }


        [HttpDelete("{packageID}")]
        public async Task<ActionResult> DeletePackageId(string packageID)
        {
            var isDeleted = await _astroLabelDataService.DeletePackageId(packageID);

            if (!isDeleted)
            {
                return NotFound($"Package with ID '{packageID}' not found.");
            }

            return Ok(true);
        }

        [HttpGet("count/{palletID}")]
        public async Task<ActionResult> CountPalletID(string palletID)
        {
            var count = await _astroLabelDataService.CountPalletID(palletID);
            return Ok(count);
        }

        [HttpGet("printPallet/{palletID}")]
        public async Task<ActionResult> PrintPallet(string palletID)
        {
            var check = await _astroLabelDataService.PrintPallet(palletID);
            return Ok(check);
        }

        [HttpPost("printPalletWithBody/{palletID}")]
        public async Task<ActionResult> PrintPalletWithBody(string palletID, [FromBody] LabelDataRequest request)
        {
            var check = await _astroLabelDataService.PrintPalletWithBody(palletID, request);
            return Ok(check);
        }

        [HttpGet("getPallet/{palletID}")]
        public async Task<ActionResult> GetPalltet(string palletID)
        {
            var list = await _astroLabelDataService.GetPallet(palletID);
            return Ok(list);
        }

        [HttpGet("deleteInPallet/{serial}")]
        public async Task<ActionResult> DeleteInPallet(string serial)
        {
            await _astroLabelDataService.DeleteBox(serial);
            return Ok(true);
        }

        [HttpPost("walterlog")]
        public async Task<IActionResult> Walterlog([FromBody] LogRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Content is required.");

            try
            {
                var id = await _walterLogService.LogAsync(request.Content, ct);
                return Ok(new { id });
            }
            catch
            {
                // Có thể log exception ở đây (ILogger) nếu cần
                return StatusCode(500, "Failed to write log.");
            }
        }
    }
}