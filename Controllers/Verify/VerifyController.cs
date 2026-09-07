using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SVN_Tools.Models.Verify;
using SVN_Tools.Models.Verify.DTOs;

namespace SVN_Tools.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifyEmployeeDataController : ControllerBase
    {
        private readonly IVerifyEmployeeDataService _verifyEmployeeDataService;
        private readonly IIotVerifyEmployeeDataService _iotVerifyEmployeeDataService;
        private readonly HttpClient _httpClient;

        private readonly IHttpClientFactory _httpClientFactory;


        public VerifyEmployeeDataController(IHttpClientFactory httpClientFactory, IVerifyEmployeeDataService verifyEmployeeDataService, IIotVerifyEmployeeDataService iotVerifyEmployeeDataService, HttpClient httpClient)
        {
            _verifyEmployeeDataService = verifyEmployeeDataService;
            _iotVerifyEmployeeDataService = iotVerifyEmployeeDataService;
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VerifyEmployeeDataDto verifyEmployeeDataDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var verifyEmployeeData = new VerifyEmployeeData
            {
                SVNCODE = verifyEmployeeDataDto.SVNCODE,
                Date = verifyEmployeeDataDto.Date,
                Area = verifyEmployeeDataDto.Area,
            };

            var createdData = await _verifyEmployeeDataService.CreateAsync(verifyEmployeeData);

            if (createdData == null)
            {
                return Ok(false);
            }
            else
            {
                return Ok(true);
            }
        }

        [HttpGet("{svnCode}")]
        public async Task<ActionResult<List<VerifyEmployeeData>>> GetBySvnCode(string svnCode)
        {
            var data = await _verifyEmployeeDataService.GetBySvnCode(svnCode);

            if (data == null)
            {
                return NotFound();
            }
            return Ok(data);
        }

        [HttpGet("verify/{svnCode}")]
        public async Task<ActionResult<List<VerifyEmployeeData>>> CheckUserValid(string svnCode)
        {
            var data = await _iotVerifyEmployeeDataService.GetByEmpCode(svnCode);

            if (data == null)
            {
                return Ok(false);
            }
            else
            {
                return Ok(true);
            }
        }

        [HttpGet]
        public async Task<List<VerifyEmployeeData>> GetAllAsync()
        {
            var data = await _verifyEmployeeDataService.GetAllAsync();
            return data;
        }

        [HttpGet("getName/{svnCode}")]
        public async Task<ActionResult<List<VerifyEmployeeData>>> GetEmployeeName(string svnCode)
        {
            var url = $"http://10.10.99.10:8108/api/odoo/searchname?nameEmployer={svnCode}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var content = await response.Content.ReadAsStringAsync();

            // Trả JSON thẳng ra
            return Content(content, "application/json");
        }

        // GET /api/verify/GetProductItemByCode?code=SERIAL
        [HttpGet("GetProductItemByCode")]
        public async Task<IActionResult> GetProductItemByCode([FromQuery] string code, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { ok = false, message = "Thiếu tham số code" });

            var fullUrl = $"http://10.10.99.10:8101/api/ViindooConnect/GetProductItemByCode?code={Uri.EscapeDataString(code)}";

            var client = _httpClientFactory.CreateClient(); // không dùng BaseAddress, gọi full URL
            client.Timeout = TimeSpan.FromSeconds(8);

            using var req = new HttpRequestMessage(HttpMethod.Get, fullUrl);

            HttpResponseMessage resp;
            try
            {
                resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new { ok = false, message = "Gateway Timeout khi gọi upstream" });
            }
            catch (Exception ex)
            {
                return StatusCode(502, new { ok = false, message = "Lỗi proxy: " + ex.Message });
            }

            var contentType = resp.Content.Headers.ContentType?.ToString()
                              ?? "application/json; charset=utf-8";
            var body = await resp.Content.ReadAsStringAsync(ct);

            Response.ContentType = contentType;
            Response.StatusCode = (int)resp.StatusCode;
            return Content(body, contentType, Encoding.UTF8);
        }
    }

}