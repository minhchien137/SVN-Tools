using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using PrinterServices.Objects;
using SVN_Tools.DAL.DataPortal;
using SVN_Tools.Models;
using SVN_Tools.Models.Label;
using SVN_Tools.Models.Utils;
using SVN_Tools.Services;
using SVN_Tools.Services.Configurations;
using SVN_Tools.Services.Utils;

namespace SVN_Tools.Controllers
{
    public class UtilsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        DBConfiguration _dBConfiguration;
        string connectionString;

        private readonly LabelService _labelService;
        private readonly PrinterService _printerService;
        private readonly LabelInfoService _labelInfoService;



        private readonly AppDbContext _context;



        public UtilsController(AppDbContext context, LabelInfoService labelInfoService, IHttpClientFactory httpClientFactory, DBConfiguration dBConfiguration, LabelService labelService, PrinterService printerService)
        {
            _httpClientFactory = httpClientFactory;
            _dBConfiguration = dBConfiguration;
            connectionString = dBConfiguration.GetConnectionString();
            _labelService = labelService;
            _printerService = printerService;
            _labelInfoService = labelInfoService;
            _context = context;

        }


        // [HttpPost]
        // public IActionResult GenerateZPL([FromBody] QRRequest request)
        // {
        //     try
        //     {
        //         if (string.IsNullOrEmpty(request.Content))
        //         {
        //             return BadRequest("Content không được để trống");
        //         }

        //         string zplCode;

        //         // Sử dụng method với các tham số tùy chỉnh nếu chúng được cung cấp
        //         if (request.XPosition.HasValue && request.YPosition.HasValue && request.PixelsPerModule.HasValue)
        //         {
        //             zplCode = _qrService.GenerateZplFromText(
        //                 request.Content,
        //                 request.XPosition.Value,
        //                 request.YPosition.Value,
        //                 request.PixelsPerModule.Value
        //             );
        //         }
        //         else
        //         {
        //             // Sử dụng method gốc với các tham số mặc định
        //             zplCode = _qrService.GenerateZplFromText(request.Content);
        //         }

        //         return Ok(new QRResponse
        //         {
        //             Success = true,
        //             ZplCode = zplCode,
        //             Message = "Tạo ZPL thành công"
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, new QRResponse
        //         {
        //             Success = false,
        //             Message = $"Lỗi: {ex.Message}"
        //         });
        //     }
        // }

        [HttpPost]
        public async Task<IActionResult> CreateLabel([FromBody] LabelDataRequest request)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(request.id);


            string result = _labelService.GenerateTemplate(printerData, request);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "image/png");

            string content = Uri.EscapeDataString(result);

            var url = $"http://api.labelary.com/v1/printers/{request.size}dpmm/labels/{request.width}x{request.height}/0/{content}";

            if (printerData.target == "Whoop" || printerData.target == "Whoopnew")
            {
                string qrBlock = _labelService.CreateQrCode(request.upcNumber, printerData.ID_Printer);
                result = result.Replace("{qrBlock}", qrBlock);
            }

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                // **Bước 1: Tạo tên file và đường dẫn để lưu**
                // Đảm bảo thư mục "Images" tồn tại hoặc tạo nó nếu cần
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images/label");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"label_{Guid.NewGuid()}.png"; // Tạo tên file duy nhất
                string filePath = Path.Combine(uploadsFolder, fileName);

                // **Bước 2: Ghi mảng byte vào file**
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // **Bước 3: Trả về đường dẫn của file hoặc thông báo thành công**
                // Bạn có thể trả về một đối tượng JSON chứa đường dẫn hoặc một thông báo
                return Ok(new { Message = "Image saved successfully", ImagePath = $"/Images/label/{fileName}", template = result });
            }
            else
            {
                throw new HttpRequestException($"Status: {response.StatusCode}, Message: {await response.Content.ReadAsStringAsync()}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreateLabelNoPreview([FromBody] LabelDataRequest request)
        {
            PrinterData printerData = new PrinterData();

            if (request.zebra == "ZT411")
            {
                printerData = await _printerService.ReadByID("Walter_411");
            }
            else
            {
                printerData = await _printerService.ReadByID(request.id);
            }
            string result = _labelService.GenerateTemplate(printerData, request);


            if (printerData.target == "Whoop" || printerData.target == "Whoopnew")
            {
                string qrBlock = _labelService.CreateQrCode(request.upcNumber, printerData.ID_Printer);
                result = result.Replace("{qrBlock}", qrBlock);
            }

            if (printerData.target == "WalterInBox")
            {
                string lotSuffix = request.LN.Substring(request.LN.Length - 5);
                string content =
    $"{{\"GTPN\":\"{request.GTPN}\"," +
    $"\"VDPN\":\"{request.VDPN}\"," +
    $"\"VDCODE\":\"29135\"," +
    $"\"LN\":\"{request.LN}\"," +
    $"\"DC\":\"{request.DC}\"," +
    $"\"LNVD\":\"{request.LN}-29135-{request.DC}\"," +
    $"\"QTY\":\"{request.quantity}\"," +
    $"\"SPN\":\"{request.GTPN}-{request.DC}-{lotSuffix}\"}}";

                string qrBlock = _labelService.CreateQrCode(content, printerData.ID_Printer);
                result = result.Replace("{qrBlock}", qrBlock);
            }

            return Ok(new { Message = "Template created successfully", template = result });

        }


        [HttpPost]
        public async Task<IActionResult> GetImageFromLabelary([FromBody] ZplRequest request)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "image/png");

            //string content = Uri.EscapeDataString(request.ZplString);

            var url = $"http://api.labelary.com/v1/printers/24dpmm/labels/4x6/0/{request.ZplString}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                // **Bước 1: Tạo tên file và đường dẫn để lưu**
                // Đảm bảo thư mục "Images" tồn tại hoặc tạo nó nếu cần
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images/label");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"label_{Guid.NewGuid()}.png"; // Tạo tên file duy nhất
                string filePath = Path.Combine(uploadsFolder, fileName);

                // **Bước 2: Ghi mảng byte vào file**
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // **Bước 3: Trả về đường dẫn của file hoặc thông báo thành công**
                // Bạn có thể trả về một đối tượng JSON chứa đường dẫn hoặc một thông báo
                return Ok(new { Message = "Image saved successfully", ImagePath = $"/Images/label/{fileName}" });
            }
            else
            {
                throw new HttpRequestException($"Status: {response.StatusCode}, Message: {await response.Content.ReadAsStringAsync()}");
            }

        }


        public class ZplRequest
        {
            public string ZplString { get; set; }
        }




        public async Task<IActionResult> UltimatePrint()
        {
            List<PrinterData> printerData = new List<PrinterData>();
            try
            {
                printerData = await _printerService.ReadList();
                if (printerData == null)
                {
                    printerData = new List<PrinterData>();
                }
            }
            catch
            {
                printerData = new List<PrinterData>();
            }
            ViewBag.oper = "Ultimate Print";
            return View(printerData);
        }




        [HttpGet("utils/toast/{id}")]
        public async Task<IActionResult> ToastPrinter(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }
            // ViewBag.imageUrl = await _labelService.GetTestTemplate(printerData.Size, printerData.width, printerData.height, printerData.ZPL_Temp);

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("ToastPrinter", printerData);
        }

        [HttpGet("utils/astro/{id}")]
        public async Task<IActionResult> AstroPrinter(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }
            // ViewBag.imageUrl = await _labelService.GetTestTemplate(printerData.Size, printerData.width, printerData.height, printerData.ZPL_Temp);

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("AstroPrinter", printerData);
        }

        [HttpGet("utils/wh/{id}")]
        public async Task<IActionResult> WHLabelPrinter(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }
            // ViewBag.imageUrl = await _labelService.GetTestTemplate(printerData.Size, printerData.width, printerData.height, printerData.ZPL_Temp);

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("WHLabelPrinter", printerData);
        }

        [HttpGet("utils/whoop/{id}")]
        public async Task<IActionResult> WhoopPrinter(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }
            // ViewBag.imageUrl = await _labelService.GetTestTemplate(printerData.Size, printerData.width, printerData.height, printerData.ZPL_Temp);

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("WhoopPrinter", printerData);
        }

        [HttpGet("utils/whoopnew/{id}")]
        public async Task<IActionResult> WhoopNewPrinter(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }
            // ViewBag.imageUrl = await _labelService.GetTestTemplate(printerData.Size, printerData.width, printerData.height, printerData.ZPL_Temp);

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("WhoopNewPrinter", printerData);
        }

        [HttpGet("utils/walterinbox/{id}")]
        public async Task<IActionResult> WalterInBoxPrinter(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }
            // ViewBag.imageUrl = await _labelService.GetTestTemplate(printerData.Size, printerData.width, printerData.height, printerData.ZPL_Temp);

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("WalterInBoxPrinter", printerData);
        }


        [HttpGet("utils/toastOneSerial/{id}")]
        public async Task<IActionResult> ToastPrinterOneSerial(string id)
        {
            PrinterData printerData = new PrinterData();
            printerData = await _printerService.ReadByID(id);
            if (printerData == null)
            {
                return NotFound();
            }

            ViewBag.oper = $"Print Details for ID: {id}";
            return View("ToastOneSerialPrinternew", printerData);
        }

        [HttpGet("utils/scantocheck/whoop")]
        public async Task<IActionResult> ScanToCheckWhoop()
        {
            return View("ScanToCheckWhoop");
        }

        [HttpGet("utils/scantocheckwithwo/whoop")]
        public async Task<IActionResult> ScanToCheckWhoopWithWO()
        {
            return View("ScanToCheckWhoopWithWO");
        }



        [HttpPost]
        public async Task<IActionResult> CheckWhoopLabel([FromBody] CheckWhoopLabelRequest request)
        {
            var upc = request.upc_number;

            // WhoopLabel item = new WhoopLabel();
            WhoopLabel[] item = await _labelInfoService.ReadByUpcNumber(upc);


            return Ok(new { item });
        }

        public class CheckWhoopLabelRequest
        {
            public string upc_number { get; set; }
        }


        [HttpPost]
        public async Task<IActionResult> CheckToastOneSerialNumber([FromBody] CheckToastOneSerialRequest request)
        {
            var serialNumber = request.serialNumber;
            var result = await _labelInfoService.CheckToastOneSerialNumber(serialNumber);
            return Ok(new { result });
        }

        public class CheckToastOneSerialRequest
        {
            public string serialNumber { get; set; }
        }

        [HttpGet("utils/check-whoop-light")]
        public async Task<IActionResult> CheckWhoopAndLight()
        {
            // bool isMatch = request.InputSerial == request.ExpectedSerial;

            string apiBase = "http://10.10.99.10:8101/api/YeelightService";
            string ip = "192.168.2.203";
            int port = 55443;

            using var client = new HttpClient();
            // Bật đèn đỏ
            var powerOnBody = new
            {
                ip,
                port,
                brightness = 80,
                power = true,
                color = "Red"
            };
            await client.PostAsJsonAsync($"{apiBase}/SetPower", powerOnBody);
            return Ok(new { success = true });
        }

        [HttpGet("utils/turn-off-light")]
        public async Task<IActionResult> TurnOffLight()
        {
            string apiBase = "http://10.10.99.10:8101/api/YeelightService";
            string ip = "192.168.2.203";
            int port = 55443;

            using var client = new HttpClient();

            var powerOffBody = new
            {
                ip,
                port,
                brightness = 0,
                power = false,
                color = ""
            };

            await client.PostAsJsonAsync($"{apiBase}/SetPower", powerOffBody);

            return Ok(new { success = true });
        }


        [HttpGet("utils/blink-light")]
        public async Task<IActionResult> BlinkLight()
        {
            string apiBase = "http://10.10.99.10:8101/api/YeelightService";
            string ip = "192.168.2.203";
            int port = 55443;

            using var client = new HttpClient();

            var powerOffBody = new
            {
                ip,
                port,
                brightness = 80,
                power = true,
                color = "Red"
            };

            await client.PostAsJsonAsync($"{apiBase}/SetBlinkColor", powerOffBody);

            return Ok(new { success = true });
        }

        public class CheckSerialRequest
        {
            public string InputSerial { get; set; }
            public string ExpectedSerial { get; set; }
        }

        [HttpGet("utils/scan_pass_check_whoop/{password}")]
        public async Task<IActionResult> ScanPassCheckWhoop(string password)
        {
            // Lấy về toàn bộ đối tượng (row) đầu tiên khớp điều kiện, hoặc null nếu không tìm thấy.
            var passwordEntry = _context.ProjectPasswordModels
                                        .FirstOrDefault(p => p.Password_name == "scan_pass_check_whoop");

            // Kiểm tra và lấy giá trị
            string passwordValue = passwordEntry?.Password_value;

            if (passwordValue == null) return Ok(false);
            if (passwordValue != password)
            {
                return Ok(false);
            }
            return Ok(true);
        }




        //FTC và FQC

        [HttpGet("utils/fctscan/toast")]
        public async Task<IActionResult> FCTScanToast()
        {
            return View("FctScanToast");
        }


        [HttpPost]
        // [ValidateAntiForgeryToken]
        [Route("FctScanToast/Submit")]
        public async Task<IActionResult> Submit([FromBody] FctSubmitReq req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Serial))
                return BadRequest(new { ok = false, message = "Serial rỗng." });

            var serial = req.Serial.Trim();
            var status = (req.Status ?? "").Trim().ToUpperInvariant();

            if (serial.Length != 13)
                return BadRequest(new { ok = false, message = "Serial phải 13 ký tự." });

            if (status != "OK" && status != "NG")
                return BadRequest(new { ok = false, message = "Trạng thái không hợp lệ." });

            // Validate theo rule dang active (neu co)
            var activeRule = await _context.SVNToastScanRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IsActive);

            if (activeRule != null)
            {
                if (!serial.StartsWith(activeRule.Prefix, StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { ok = false, message = $"Serial khong khop prefix '{activeRule.Prefix}'." });

                var suffixStr = serial[activeRule.Prefix.Length..];
                if (!int.TryParse(suffixStr, out var seq) || seq < activeRule.MinSeq)
                    return BadRequest(new { ok = false, message = $"So cuoi phai tu {activeRule.MinSeq:D4} tro len." });
            }

            var exists = await _context.SVNToastSerialInfos
                                       .AsNoTracking()
                                       .AnyAsync(x => x.SerialNumber == serial);

            if (exists)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { ok = false, message = $"Serial đã tồn tại: {serial}" });
            }

            var nowVN = GetVietnamNow();

            // === Step 1: Lấy WorkOrder + woPageId từ Odoo ===
            string? workOrder = null;
            int? woPageId = null;

            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

                var response = await client.GetAsync($"/api/Odoo/getworkorderfromserial/{serial}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("workOrder", out var woProp))
                        workOrder = woProp.GetString();

                    // if (doc.RootElement.TryGetProperty("woPageId", out var idProp) &&
                    //     idProp.ValueKind == JsonValueKind.Number)
                    //     woPageId = idProp.GetInt32();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi lấy WorkOrder từ Odoo: {ex.Message}");
            }

            // === Step 2: Insert vào DB ===
            var rec = new SVNToastSerialInfo
            {
                SerialNumber = serial,
                FCTStatus = status,
                FCTStatusDatetime = nowVN,
                WorkOrder = workOrder
            };

            try
            {
                _context.SVNToastSerialInfos.Add(rec);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return StatusCode(StatusCodes.Status409Conflict,
                    new { ok = false, message = $"Serial đã tồn tại: {serial}" });
            }

            // === Step 3: Gửi comment Odoo (qua API nội bộ) ===
            // if (woPageId.HasValue)
            // {
            //     try
            //     {
            //         var body = $"FCT: {status} — {nowVN:yyyy-MM-dd HH:mm:ss}";
            //         using var client = new HttpClient();
            //         client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

            //         var resp = await client.PostAsJsonAsync("/api/Odoo/postcomment", new
            //         {
            //             ThreadId = woPageId.Value,
            //             Body = body
            //         });

            //         Console.WriteLine(resp.IsSuccessStatusCode
            //             ? $"✅ Gửi comment FCT thành công: {body}"
            //             : $"⚠️ Gửi comment FCT thất bại: {resp.StatusCode}");
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine($"⚠️ Lỗi khi gửi comment FCT: {ex.Message}");
            //     }
            // }

            return Ok(new { ok = true, serial, status, workOrder });
        }

        private static DateTime GetVietnamNow()
        {
            var tzId = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "SE Asia Standard Time" : "Asia/Bangkok";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateFqcStatus([FromBody] FqcUpdateRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.serialNumber) || string.IsNullOrWhiteSpace(req.status))
                return BadRequest(new { ok = false, message = "Thiếu dữ liệu." });

            try
            {
                var serial = req.serialNumber.Trim();
                var status = req.status.ToUpperInvariant().Trim();

                // 1️⃣ Tìm record trong DB
                var rec = await _context.SVNToastSerialInfos
                                        .FirstOrDefaultAsync(x => x.SerialNumber == serial);

                if (rec == null)
                    return BadRequest(new { ok = false, message = $"Serial {serial} chưa qua FCT." });

                if (rec.FCTStatus?.ToUpper() == "NG")
                    return BadRequest(new { ok = false, message = $"Serial {serial} có trạng thái FCT là NG." });

                if (!string.IsNullOrWhiteSpace(rec.FQCStatus))
                    return BadRequest(new { ok = false, message = $"Serial {serial} đã có FQC status." });

                // 2️⃣ Cập nhật FQC
                var nowVN = DateTime.UtcNow.AddHours(7);
                rec.FQCStatus = status;
                rec.FQCStatusDatetime = nowVN;

                await _context.SaveChangesAsync();

                // 3️⃣ Lấy woPageId
                // int? woPageId = null;
                // try
                // {
                //     using var client = new HttpClient();
                //     client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
                //     var resp = await client.GetAsync($"/api/Odoo/getworkorderfromserial/{serial}");
                //     if (resp.IsSuccessStatusCode)
                //     {
                //         var json = await resp.Content.ReadAsStringAsync();
                //         using var doc = JsonDocument.Parse(json);
                //         if (doc.RootElement.TryGetProperty("woPageId", out var woProp))
                //             woPageId = woProp.GetInt32();
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine($"⚠️ Lỗi lấy woPageId: {ex.Message}");
                // }

                // 4️⃣ Gửi comment Odoo (qua API nội bộ)
                // if (woPageId.HasValue)
                // {
                //     try
                //     {
                //         var body = $"FQC: {status} — {nowVN:yyyy-MM-dd HH:mm:ss}";
                //         using var client = new HttpClient();
                //         client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

                //         var resp = await client.PostAsJsonAsync("/api/Odoo/postcomment", new
                //         {
                //             ThreadId = woPageId.Value,
                //             Body = body
                //         });

                //         Console.WriteLine(resp.IsSuccessStatusCode
                //             ? $"✅ Gửi comment FQC thành công: {body}"
                //             : $"⚠️ Gửi comment FQC thất bại: {resp.StatusCode}");
                //     }
                //     catch (Exception ex)
                //     {
                //         Console.WriteLine($"⚠️ Lỗi khi gửi comment FQC: {ex.Message}");
                //     }
                // }

                return Ok(new
                {
                    ok = true,
                    message = $"Cập nhật FQC thành công ({status}) cho serial: {serial}.",
                    serial,
                    time = nowVN
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Lỗi khi cập nhật FQC", details = ex.Message });
            }
        }

        [HttpGet("utils/CheckToastPalletSerial")]
        public async Task<IActionResult> CheckToastPalletSerial()
        {
            return View("CheckToastPalletSerial");
        }

        [HttpGet("utils/GetInfoSerial")]
        public async Task<IActionResult> GetInfoSerial()
        {
            return View("GetInfoSerial");
        }

        [HttpGet("GetInfoSerial/{serial}")]
        public async Task<IActionResult> GetInfoSerialDetail(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial) || serial.Length != 13)
                return BadRequest(new { ok = false, message = "Serial không hợp lệ." });

            var item = await _context.SVNToastSerialInfos
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(x => x.SerialNumber == serial);

            if (item == null)
                return NotFound(new { ok = false, message = "Không tìm thấy serial này." });

            return Ok(new
            {
                ok = true,
                data = new
                {
                    item.SerialNumber,
                    item.WorkOrder,
                    item.FCTStatus,
                    item.FCTStatusDatetime,
                    item.FQCStatus,
                    item.FQCStatusDatetime
                }
            });
        }

        [HttpGet("GetInfoSerial/ExportExcel/{prefix}")]
        public async Task<IActionResult> ExportExcel(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix) || prefix.Length != 9)
                return BadRequest("Prefix không hợp lệ.");

            var data = await _context.SVNToastSerialInfos
                .AsNoTracking()
                .Where(x => x.SerialNumber.StartsWith(prefix))
                .Select(x => new
                {
                    x.SerialNumber,
                    x.WorkOrder,
                    x.FCTStatus,
                    x.FCTStatusDatetime,
                    x.FQCStatus,
                    x.FQCStatusDatetime
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound("Không có dữ liệu.");

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Serial Info");

            // header
            ws.Cell(1, 1).Value = "Serial Number";
            ws.Cell(1, 2).Value = "Work Order";
            ws.Cell(1, 3).Value = "FCT Status";
            ws.Cell(1, 4).Value = "FCT Time";
            ws.Cell(1, 5).Value = "FQC Status";
            ws.Cell(1, 6).Value = "FQC Time";

            // nội dung
            int row = 2;
            foreach (var item in data)
            {
                ws.Cell(row, 1).Value = item.SerialNumber;
                ws.Cell(row, 2).Value = item.WorkOrder;
                ws.Cell(row, 3).Value = item.FCTStatus;
                ws.Cell(row, 4).Value = item.FCTStatusDatetime?.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cell(row, 5).Value = item.FQCStatus;
                ws.Cell(row, 6).Value = item.FQCStatusDatetime?.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var fileName = $"Serial_{prefix}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // [HttpPost("utils/UpdateSerialStatus")]
        // public async Task<IActionResult> UpdateSerialStatus([FromBody] UpdateSerialStatusRequest req)
        // {
        //     if (string.IsNullOrWhiteSpace(req.Serial) || string.IsNullOrWhiteSpace(req.SVNCode))
        //         return BadRequest(new { ok = false, message = "Thiếu dữ liệu đầu vào." });

        //     var rec = await _context.SVNToastSerialInfos.FirstOrDefaultAsync(x => x.SerialNumber == req.Serial);
        //     if (rec == null)
        //         return NotFound(new { ok = false, message = "Không tìm thấy serial." });

        //     var nowVN = DateTime.UtcNow.AddHours(7);
        //     bool updated = false;

        //     if (!string.IsNullOrWhiteSpace(req.FCT))
        //     {
        //         rec.FCTStatus = req.FCT.ToUpperInvariant();
        //         rec.FCTStatusDatetime = nowVN;
        //         updated = true;
        //     }

        //     if (!string.IsNullOrWhiteSpace(req.FQC))
        //     {
        //         rec.FQCStatus = req.FQC.ToUpperInvariant();
        //         rec.FQCStatusDatetime = nowVN;
        //         updated = true;
        //     }

        //     if (!updated)
        //         return BadRequest(new { ok = false, message = "Không có trường nào được chọn để cập nhật." });

        //     rec.updateBySVNCode = req.SVNCode;
        //     await _context.SaveChangesAsync();

        //     return Ok(new { ok = true, message = $"Đã cập nhật {req.Serial} thành công.", time = nowVN });
        // }

        [HttpPost("utils/UpdateSerialStatus")]
        public async Task<IActionResult> UpdateSerialStatus([FromBody] UpdateSerialStatusRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Serial) || string.IsNullOrWhiteSpace(req.SVNCode))
                return BadRequest(new { ok = false, message = "Thiếu dữ liệu đầu vào." });

            var rec = await _context.SVNToastSerialInfos
                .FirstOrDefaultAsync(x => x.SerialNumber == req.Serial);

            if (rec == null)
                return NotFound(new { ok = false, message = "Không tìm thấy serial." });

            var nowVN = DateTime.UtcNow.AddHours(7);
            bool updated = false;

            if (!string.IsNullOrWhiteSpace(req.FCT))
            {
                rec.FCTStatus = req.FCT.ToUpperInvariant();
                rec.FCTStatusDatetime = nowVN;
                updated = true;
            }

            if (!string.IsNullOrWhiteSpace(req.FQC))
            {
                rec.FQCStatus = req.FQC.ToUpperInvariant();
                rec.FQCStatusDatetime = nowVN;
                updated = true;
            }

            if (!updated)
                return BadRequest(new { ok = false, message = "Không có trường nào được chọn để cập nhật." });

            rec.updateBySVNCode = req.SVNCode;
            await _context.SaveChangesAsync();

            // 🟡 3️⃣ Lấy work order page ID (woPageId)
            int? woPageId = null;
            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");
                var resp = await client.GetAsync($"/api/Odoo/getworkorderfromserial/{req.Serial}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("woPageId", out var woProp))
                        woPageId = woProp.GetInt32();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi lấy woPageId: {ex.Message}");
            }

            // 🟢 4️⃣ Gửi comment tới Odoo (nếu có thread)
            if (woPageId.HasValue)
            {
                try
                {
                    var fctText = !string.IsNullOrWhiteSpace(rec.FCTStatus)
                        ? $"FCT: {rec.FCTStatus} — {rec.FCTStatusDatetime:yyyy-MM-dd HH:mm:ss}"
                        : "FCT: (không thay đổi)";

                    var fqcText = !string.IsNullOrWhiteSpace(rec.FQCStatus)
                        ? $"FQC: {rec.FQCStatus} — {rec.FQCStatusDatetime:yyyy-MM-dd HH:mm:ss}"
                        : "FQC: (không thay đổi)";

                    var body = $"Updated: {fctText} | {fqcText} | Người cập nhật: {req.SVNCode}";

                    using var client = new HttpClient();
                    client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

                    var resp = await client.PostAsJsonAsync("/api/Odoo/postcomment", new
                    {
                        ThreadId = woPageId.Value,
                        Body = body
                    });

                    Console.WriteLine(resp.IsSuccessStatusCode
                        ? $"✅ Gửi comment thành công: {body}"
                        : $"⚠️ Gửi comment thất bại: {resp.StatusCode}");

                    if (rec.FQCStatus?.ToUpper() == "OK")
                    {
                        rec.FQCStatus = null;
                        // KHÔNG sửa rec.FQCStatusDatetime (giữ nguyên)
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"🔄 FQCStatus của {req.Serial} đã reset về null sau khi gửi comment.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Lỗi khi gửi comment: {ex.Message}");
                }
            }

            return Ok(new
            {
                ok = true,
                message = $"Đã cập nhật {req.Serial} thành công.",
                time = nowVN
            });
        }

        [HttpGet("utils/commenttowo")]
        public IActionResult CommentPage()
        {
            return View("PostComment");
        }

        // Toast Lookup

        [HttpGet("utils/toast-lookup")]
        public IActionResult ToastLookup() => View("ToastLookup");

        [HttpGet("api/ToastLookup")]
        public async Task<IActionResult> GetToastLookup([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { ok = false, message = "Query rong." });

            q = q.Trim().ToUpper();

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);

            // SP_Toast_SearchSummary: tìm mọi trường, trả về summary list
            var summaries = (await Dapper.SqlMapper.QueryAsync(conn,
                "SP_Toast_SearchSummary", new { q },
                commandType: System.Data.CommandType.StoredProcedure)).ToList();

            if (!summaries.Any())
                return Ok(new { ok = true, found = false });

            if (summaries.Count > 1)
            {
                // Nếu query khớp chính xác 1 serial → hiện detail luôn, bỏ qua list
                var exact = summaries.FirstOrDefault(s =>
                    string.Equals((string)s.serial_number, q, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return await GetToastSerialDetail((string)exact.serial_number, conn);

                return Ok(new { ok = true, found = true, mode = "list", serials = summaries });
            }

            // Đúng 1 kết quả → SP_Toast_GetSerialDetail
            var only = summaries[0];
            bool isDirectSerialMatch = string.Equals((string)only.serial_number, q, StringComparison.OrdinalIgnoreCase);
            return await GetToastSerialDetail((string)only.serial_number, conn,
                isDirectSerialMatch ? null : (string?)only.match_types,
                isDirectSerialMatch ? null : q);
        }

        [HttpGet("api/ToastLookup/detail")]
        public async Task<IActionResult> GetToastLookupDetail([FromQuery] string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
                return BadRequest(new { ok = false, message = "Serial rong." });

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            return await GetToastSerialDetail(serial.Trim().ToUpper(), conn);
        }

        private static List<int> ExtractComponentProductIds(string? componentListJson)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(componentListJson)) return ids;
            try
            {
                using var doc = JsonDocument.Parse(componentListJson);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("product_id", out var pidEl) && pidEl.TryGetInt32(out var pid))
                        ids.Add(pid);
                }
            }
            catch { }
            return ids;
        }

        private sealed class ComponentInfo
        {
            public int ProductId { get; set; }
            public string? ProductCode { get; set; }
            public string? ProductName { get; set; }
            public string LotNumber { get; set; } = "";
        }

        private static List<ComponentInfo> EnrichComponentList(string? componentListJson, Dictionary<int, (string? Code, string? Name)> productMap)
        {
            var result = new List<ComponentInfo>();
            if (string.IsNullOrWhiteSpace(componentListJson)) return result;
            try
            {
                using var doc = JsonDocument.Parse(componentListJson);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    int productId = el.TryGetProperty("product_id", out var pidEl) && pidEl.TryGetInt32(out var pid) ? pid : 0;
                    string lotNumber = el.TryGetProperty("lotNumber", out var lotEl) ? (lotEl.GetString() ?? "") : "";
                    productMap.TryGetValue(productId, out var info);

                    result.Add(new ComponentInfo
                    {
                        ProductId = productId,
                        ProductCode = info.Code,
                        ProductName = info.Name,
                        LotNumber = lotNumber
                    });
                }
            }
            catch { }
            return result;
        }

        private async Task<IActionResult> GetToastSerialDetail(string serial, System.Data.SqlClient.SqlConnection conn,
            string? matchTypes = null, string? matchQuery = null)
        {
            using var multi = await Dapper.SqlMapper.QueryMultipleAsync(conn,
                "SP_Toast_GetSerialDetail", new { serial },
                commandType: System.Data.CommandType.StoredProcedure);

            var fctFqcRow = (await multi.ReadAsync()).FirstOrDefault();
            var palletRow = (await multi.ReadAsync()).FirstOrDefault();
            var prodRows  = (await multi.ReadAsync()).Cast<dynamic>().ToList();

            // FG: serial nằm trong component_list (bị tiêu thụ làm linh kiện)
            // WIP: serial là sản phẩm đầu ra, không nằm trong component_list
            var wip = prodRows.FirstOrDefault(x => !((string?)x.component_list ?? "").Contains(serial));
            var fg  = prodRows.FirstOrDefault(x =>  ((string?)x.component_list ?? "").Contains(serial));

            var productIds = ExtractComponentProductIds(wip == null ? null : (string?)wip.component_list)
                .Concat(ExtractComponentProductIds(fg == null ? null : (string?)fg.component_list))
                .Distinct()
                .ToList();

            var productMap = new Dictionary<int, (string? Code, string? Name)>();
            if (productIds.Count > 0)
            {
                var productRows = await Dapper.SqlMapper.QueryAsync(conn,
                    "SELECT pp.id, pp.default_code, pt.name AS product_name FROM SVN_product_product pp " +
                    "LEFT JOIN SVN_product_template_1 pt ON pp.product_tmpl_id = pt.id WHERE pp.id IN @ids",
                    new { ids = productIds });
                foreach (var r in productRows)
                    productMap[(int)r.id] = ((string?)r.default_code, (string?)r.product_name);
            }

            var wipComponents = EnrichComponentList(wip == null ? null : (string?)wip.component_list, productMap);
            var fgComponents  = EnrichComponentList(fg == null ? null : (string?)fg.component_list, productMap);

            var renamedTo = await _context.SVNToastEditLogs
                .Where(x => x.ActionType == ToastEditActionType.RenameSerial && x.SerialCode == serial)
                .OrderByDescending(x => x.EditedAt)
                .FirstOrDefaultAsync();
            var renamedFrom = await _context.SVNToastEditLogs
                .Where(x => x.ActionType == ToastEditActionType.RenameSerial && x.RelatedSerial == serial)
                .OrderByDescending(x => x.EditedAt)
                .FirstOrDefaultAsync();

            object? renameInfo = (renamedTo == null && renamedFrom == null) ? null : new
            {
                renamedTo = renamedTo == null ? null : new
                {
                    newSerial = renamedTo.RelatedSerial,
                    reason    = renamedTo.Reason,
                    editedBy  = renamedTo.EditedBy,
                    editedAt  = renamedTo.EditedAt
                },
                renamedFrom = renamedFrom == null ? null : new
                {
                    oldSerial = renamedFrom.SerialCode,
                    reason    = renamedFrom.Reason,
                    editedBy  = renamedFrom.EditedBy,
                    editedAt  = renamedFrom.EditedAt
                }
            };

            object? matchInfo = null;
            if (!string.IsNullOrWhiteSpace(matchTypes))
            {
                var matchedComponents = new List<object>();
                if (matchTypes.Contains("Lot") && !string.IsNullOrWhiteSpace(matchQuery))
                {
                    void CollectLotMatches(List<ComponentInfo> list, string station)
                    {
                        foreach (var c in list)
                        {
                            if (!string.IsNullOrEmpty(c.LotNumber) &&
                                c.LotNumber.Contains(matchQuery, StringComparison.OrdinalIgnoreCase))
                            {
                                matchedComponents.Add(new
                                {
                                    station,
                                    productCode = c.ProductCode,
                                    productName = c.ProductName,
                                    lotNumber = c.LotNumber
                                });
                            }
                        }
                    }
                    CollectLotMatches(wipComponents, "Trạm WIP");
                    CollectLotMatches(fgComponents, "Trạm FG");
                }

                matchInfo = new
                {
                    types = matchTypes,
                    query = matchQuery,
                    components = matchedComponents.Count > 0 ? matchedComponents : null
                };
            }

            return Ok(new
            {
                ok     = true,
                found  = true,
                mode   = "detail",
                serial,
                matchInfo,
                renameInfo,
                fctFqc = fctFqcRow == null ? null : new
                {
                    workOrder   = (string?)fctFqcRow.work_order,
                    fctStatus   = (string?)fctFqcRow.FCT_status,
                    fctDatetime = (DateTime?)fctFqcRow.FCT_status_datetime,
                    fqcStatus   = (string?)fctFqcRow.FQC_status,
                    fqcDatetime = (DateTime?)fctFqcRow.FQC_status_datetime,
                    updatedBy   = (string?)fctFqcRow.update_by_svncode
                },
                pallet = palletRow == null ? null : new
                {
                    palletID   = (string?)palletRow.PalletID,
                    date       = (string?)palletRow.Date,
                    scanDate   = (string?)palletRow.ScanDate,
                    employeeID = (string?)palletRow.EmployeeID,
                    countSerial= (int?)palletRow.CountSerial,
                    packageID  = (string?)palletRow.PackageID,
                    serials    = ((string?)palletRow.Serial)?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                },
                wip = wip == null ? null : new
                {
                    woCode         = (string?)wip.wo_code,
                    masterWoCode   = (string?)wip.master_wo_code,
                    dateFinished   = (DateTime?)wip.date_finished,
                    status         = (string?)wip.status,
                    componentList  = wipComponents,
                    consumedWoCode = (string?)wip.consumed_wo_code
                },
                fg = fg == null ? null : new
                {
                    woCode        = (string?)fg.wo_code,
                    masterWoCode  = (string?)fg.master_wo_code,
                    dateFinished  = (DateTime?)fg.date_finished,
                    status        = (string?)fg.status,
                    componentList = fgComponents
                }
            });
        }

        // Toast Correction (sua linh kien sai / doi SN)

        [HttpGet("utils/toast-correction")]
        public IActionResult ToastCorrection() => View("ToastCorrection");

        [HttpGet("utils/toast-edit-history")]
        public IActionResult ToastEditHistory() => View("ToastEditHistory");

        [HttpGet("api/ToastCorrection/edit-log")]
        public async Task<IActionResult> GetToastEditLog(
            [FromQuery] string? q, [FromQuery] string? actionType,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.SVNToastEditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionType))
                query = query.Where(x => x.ActionType == actionType);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(x =>
                    x.SerialCode.Contains(term) ||
                    (x.RelatedSerial != null && x.RelatedSerial.Contains(term)) ||
                    (x.EditedBy != null && x.EditedBy.Contains(term)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.EditedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { ok = true, total, page, pageSize, items });
        }

        [HttpPost("api/ToastCorrection/replace-component")]
        public async Task<IActionResult> ReplaceToastComponent([FromBody] ReplaceComponentRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Serial) || string.IsNullOrWhiteSpace(req.Station) ||
                string.IsNullOrWhiteSpace(req.OldLotNumber) || string.IsNullOrWhiteSpace(req.NewProductCode) ||
                string.IsNullOrWhiteSpace(req.NewLotNumber) || string.IsNullOrWhiteSpace(req.Reason) ||
                string.IsNullOrWhiteSpace(req.SVNCode))
                return BadRequest(new { ok = false, message = "Thiếu dữ liệu đầu vào." });

            var serial = req.Serial.Trim().ToUpper();
            var station = req.Station.Trim().ToUpper();
            if (station != "WIP" && station != "FG")
                return BadRequest(new { ok = false, message = "Trạm không hợp lệ (chỉ WIP hoặc FG)." });
            var state = station == "WIP" ? "Consumed" : "Used";

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();

            var log = await Dapper.SqlMapper.QuerySingleOrDefaultAsync(conn,
                "SELECT TOP 1 id, component_list FROM SVN_ProductionInputLogs WHERE serial_code = @serial AND state = @state ORDER BY date_finished DESC",
                new { serial, state });
            if (log == null)
                return NotFound(new { ok = false, message = $"Không tìm thấy bản ghi {station} cho serial {serial}." });

            string? oldJson = (string?)log.component_list;
            if (string.IsNullOrWhiteSpace(oldJson))
                return BadRequest(new { ok = false, message = "Serial này chưa có danh sách linh kiện." });

            var productRow = await Dapper.SqlMapper.QuerySingleOrDefaultAsync(conn,
                "SELECT TOP 1 id FROM SVN_product_product WHERE default_code = @code",
                new { code = req.NewProductCode.Trim() });
            if (productRow == null)
                return BadRequest(new { ok = false, message = $"Không tìm thấy mã sản phẩm {req.NewProductCode}." });
            int newProductId = (int)productRow.id;

            JsonNode? node;
            try { node = JsonNode.Parse(oldJson); }
            catch { return BadRequest(new { ok = false, message = "Dữ liệu linh kiện hiện tại bị lỗi định dạng." }); }

            var arr = node as JsonArray;
            if (arr == null)
                return BadRequest(new { ok = false, message = "Dữ liệu linh kiện hiện tại không đúng định dạng." });

            JsonObject? target = null;
            foreach (var item in arr)
            {
                if (item is JsonObject obj &&
                    string.Equals((string?)obj["lotNumber"], req.OldLotNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    target = obj;
                    break;
                }
            }
            if (target == null)
                return NotFound(new { ok = false, message = $"Không tìm thấy linh kiện với lot {req.OldLotNumber} trong trạm {station}." });

            target["product_id"] = newProductId;
            target["lotNumber"] = req.NewLotNumber.Trim();
            string newJson = node!.ToJsonString();

            await conn.ExecuteAsync(
                "UPDATE SVN_ProductionInputLogs SET component_list = @newJson WHERE id = @id",
                new { newJson, id = (int)log.id });

            _context.SVNToastEditLogs.Add(new SVNToastEditLog
            {
                ActionType = ToastEditActionType.ReplaceComponent,
                SerialCode = serial,
                Station    = station,
                OldValue   = oldJson,
                NewValue   = newJson,
                Reason     = req.Reason.Trim(),
                EditedBy   = req.SVNCode.Trim(),
                EditedAt   = DateTime.UtcNow.AddHours(7)
            });
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, message = $"Đã cập nhật linh kiện cho serial {serial} ({station})." });
        }

        [HttpPost("api/ToastCorrection/rename-serial")]
        public async Task<IActionResult> RenameToastSerial([FromBody] RenameSerialRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.OldSerial) || string.IsNullOrWhiteSpace(req.NewSerial) ||
                string.IsNullOrWhiteSpace(req.Reason) || string.IsNullOrWhiteSpace(req.SVNCode))
                return BadRequest(new { ok = false, message = "Thiếu dữ liệu đầu vào." });

            var oldSerial = req.OldSerial.Trim().ToUpper();
            var newSerial = req.NewSerial.Trim().ToUpper();
            if (oldSerial == newSerial)
                return BadRequest(new { ok = false, message = "SN mới phải khác SN cũ." });

            var newInfo = await _context.SVNToastSerialInfos.FirstOrDefaultAsync(x => x.SerialNumber == newSerial);
            if (newInfo == null)
                return BadRequest(new { ok = false, message = $"SN mới {newSerial} chưa tồn tại trong hệ thống." });
            if (!string.Equals(newInfo.FCTStatus, "OK", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(newInfo.FQCStatus, "OK", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { ok = false, message = $"SN mới {newSerial} chưa qua đủ FCT/FQC (OK)." });

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();

            int existingLogs = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM SVN_ProductionInputLogs WHERE serial_code = @newSerial", new { newSerial });
            if (existingLogs > 0)
                return BadRequest(new { ok = false, message = $"SN mới {newSerial} đã có dữ liệu WIP/FG riêng, không thể gộp." });

            var oldPalletRows = (await conn.QueryAsync(
                "SELECT Id, Serial FROM SVN_Astro_Label_Data WHERE isDeleted = 0 AND EmployeeID = 'toast' AND Serial LIKE '%' + @newSerial + '%'",
                new { newSerial })).ToList();
            bool newAlreadyInPallet = oldPalletRows.Any(r =>
                ((string)r.Serial).Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Any(s => string.Equals(s.Trim(), newSerial, StringComparison.OrdinalIgnoreCase)));
            if (newAlreadyInPallet)
                return BadRequest(new { ok = false, message = $"SN mới {newSerial} đã nằm trong 1 pallet khác, không thể gộp." });

            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(
                    "UPDATE SVN_ProductionInputLogs SET serial_code = @newSerial WHERE serial_code = @oldSerial",
                    new { newSerial, oldSerial }, tran);

                var palletRows = (await conn.QueryAsync(
                    "SELECT Id, Serial FROM SVN_Astro_Label_Data WHERE isDeleted = 0 AND EmployeeID = 'toast' AND Serial LIKE '%' + @oldSerial + '%'",
                    new { oldSerial }, tran)).ToList();

                foreach (var row in palletRows)
                {
                    var parts = ((string)row.Serial).Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToList();
                    bool changed = false;
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (string.Equals(parts[i], oldSerial, StringComparison.OrdinalIgnoreCase))
                        {
                            parts[i] = newSerial;
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        await conn.ExecuteAsync(
                            "UPDATE SVN_Astro_Label_Data SET Serial = @serial WHERE Id = @id",
                            new { serial = string.Join(",", parts), id = (int)row.Id }, tran);
                    }
                }

                await conn.ExecuteAsync(
                    @"INSERT INTO SVN_Toast_Edit_Log (ActionType, SerialCode, RelatedSerial, Reason, EditedBy, EditedAt)
                      VALUES (@ActionType, @SerialCode, @RelatedSerial, @Reason, @EditedBy, @EditedAt)",
                    new
                    {
                        ActionType = ToastEditActionType.RenameSerial,
                        SerialCode = oldSerial,
                        RelatedSerial = newSerial,
                        Reason = req.Reason.Trim(),
                        EditedBy = req.SVNCode.Trim(),
                        EditedAt = DateTime.UtcNow.AddHours(7)
                    }, tran);

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }

            return Ok(new { ok = true, message = $"Đã chuyển dữ liệu sản xuất từ {oldSerial} sang {newSerial}." });
        }

        // Toast Scan Rules

        [HttpGet("utils/toast-scan-rules")]
        public IActionResult ToastScanRules() => View("ToastScanRules");

        [HttpGet("api/ToastScanRule")]
        public async Task<IActionResult> GetAllToastScanRules()
        {
            var rules = await _context.SVNToastScanRules
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return Ok(rules);
        }

        [HttpGet("api/ToastScanRule/active")]
        public async Task<IActionResult> GetActiveToastScanRule()
        {
            var rule = await _context.SVNToastScanRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.IsActive);
            return Ok(rule);
        }

        [HttpPost("api/ToastScanRule")]
        public async Task<IActionResult> CreateToastScanRule([FromBody] ToastScanRuleRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Prefix))
                return BadRequest(new { ok = false, message = "Prefix khong duoc rong." });
            if (req.MinSeq <= 0)
                return BadRequest(new { ok = false, message = "MinSeq phai lon hon 0." });
            if (req.Prefix.Length >= 13)
                return BadRequest(new { ok = false, message = "Prefix phai ngan hon 13 ky tu." });

            var rule = new SVNToastScanRule
            {
                Prefix = req.Prefix.Trim().ToUpperInvariant(),
                MinSeq = req.MinSeq,
                Note = req.Note?.Trim(),
                IsActive = false,
                CreatedAt = GetVietnamNow()
            };
            _context.SVNToastScanRules.Add(rule);
            await _context.SaveChangesAsync();
            return Ok(new { ok = true, rule });
        }

        [HttpPut("api/ToastScanRule/{id}/activate")]
        public async Task<IActionResult> ActivateToastScanRule(int id)
        {
            var rules = await _context.SVNToastScanRules.ToListAsync();
            var target = rules.FirstOrDefault(r => r.Id == id);
            if (target == null)
                return NotFound(new { ok = false, message = "Khong tim thay rule." });

            foreach (var r in rules) r.IsActive = (r.Id == id);
            await _context.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        [HttpPut("api/ToastScanRule/{id}/deactivate")]
        public async Task<IActionResult> DeactivateToastScanRule(int id)
        {
            var rule = await _context.SVNToastScanRules.FindAsync(id);
            if (rule == null)
                return NotFound(new { ok = false, message = "Khong tim thay rule." });

            rule.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        [HttpDelete("api/ToastScanRule/{id}")]
        public async Task<IActionResult> DeleteToastScanRule(int id)
        {
            var rule = await _context.SVNToastScanRules.FindAsync(id);
            if (rule == null)
                return NotFound(new { ok = false, message = "Khong tim thay rule." });

            _context.SVNToastScanRules.Remove(rule);
            await _context.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        public class ToastScanRuleRequest
        {
            public string Prefix { get; set; } = "";
            public int MinSeq { get; set; }
            public string? Note { get; set; }
        }

    }
}
