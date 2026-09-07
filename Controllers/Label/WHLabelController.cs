using Microsoft.AspNetCore.Mvc;
using SVN_Tools.Models.Label;

namespace SVN_Tools.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WHLabelInfoController : ControllerBase
    {
        private readonly WHLabelInfoService _service;

        public WHLabelInfoController(WHLabelInfoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Tạo mới 1 WHLabelInfo
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] WHLabelInfo dto)
        {
            if (dto == null)
                return BadRequest("Invalid data");

            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Lấy mã LabelId tiếp theo dựa vào prefix
        /// </summary>
        [HttpGet("next-id")]
        public async Task<IActionResult> GetNextId([FromQuery] string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return BadRequest("Prefix is required");

            var nextId = await _service.GetPackageListByPrefix(prefix);
            return Ok(new { NextId = nextId });
        }
    }
}
