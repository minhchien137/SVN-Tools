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

        // Toast Dashboard

        [HttpGet("utils/toast-dashboard")]
        public IActionResult ToastDashboard() => View("ToastDashboard");

        [HttpGet("api/ToastDashboard")]
        public async Task<IActionResult> GetToastDashboard([FromQuery] string? fromDate, [FromQuery] string? toDate)
        {
            var now = GetVietnamNow().Date;
            DateTime from = now, to = now;
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fd)) from = fd.Date;
            if (!string.IsNullOrWhiteSpace(toDate)   && DateTime.TryParse(toDate,   out var td)) to   = td.Date;
            if (to < from) to = from;

            try
            {
                using var conn = new System.Data.SqlClient.SqlConnection(connectionString);

                // Query 1: lấy danh sách serial trong khoảng ngày
                var serials = (await conn.QueryAsync(@"
                    SELECT serial_number AS SerialNumber, work_order AS WorkOrder,
                           FCT_status AS FctStatus, FCT_status_datetime AS FctDatetime,
                           FQC_status AS FqcStatus, FQC_status_datetime AS FqcDatetime
                    FROM SVN_Toast_Serial_Info
                    WHERE CAST(FCT_status_datetime AS DATE) BETWEEN @from AND @to
                    ORDER BY FCT_status_datetime DESC",
                    new { from, to },
                    commandTimeout: 30
                )).Cast<dynamic>().ToList();

                if (!serials.Any())
                    return Ok(new {
                        ok = true,
                        summary = new { total = 0, complete = 0, missingWip = 0, anyViol = 0 },
                        alerts = new List<object>()
                    });

                var serialList = serials.Select(s => (string)s.SerialNumber).ToList();
                var dateFrom   = from.AddDays(-60);
                var dateTo     = to.AddDays(7);

                // Query 2: WIP — lấy thêm wo_code để fill WorkOrder khi SVN_Toast_Serial_Info null
                var wipRows = (await conn.QueryAsync(@"
                    SELECT DISTINCT serial_code AS SerialCode, MAX(wo_code) AS WoCode
                    FROM SVN_ProductionInputLogs
                    WHERE serial_code IN @serialList
                      AND date_finished >= @dateFrom
                      AND (component_list IS NULL
                           OR (component_list NOT LIKE '%""lotNumber"":""' + serial_code + '""%'
                               AND component_list NOT LIKE '%""lotNumber"": ""' + serial_code + '""%'))
                    GROUP BY serial_code",
                    new { serialList, dateFrom },
                    commandTimeout: 30
                )).Cast<dynamic>().ToList();

                var wipSet   = new HashSet<string>(wipRows.Select(w => (string)w.SerialCode), StringComparer.OrdinalIgnoreCase);
                var wipWoMap = wipRows.ToDictionary(w => (string)w.SerialCode, w => (string?)w.WoCode, StringComparer.OrdinalIgnoreCase);

                // Query 3: FG — chỉ lấy records của các serial đang xét (serial_code IN serialList)
                // để tránh nhầm serial X là "có FG" chỉ vì X xuất hiện là component trong record của serial Y khác
                var componentRows = (await conn.QueryAsync(@"
                    SELECT serial_code, component_list
                    FROM SVN_ProductionInputLogs
                    WHERE date_finished BETWEEN @dateFrom AND @dateTo
                      AND serial_code IN @serialList
                      AND component_list IS NOT NULL
                      AND LEN(component_list) > 2",
                    new { dateFrom, dateTo, serialList },
                    commandTimeout: 60
                )).Cast<dynamic>().ToList();

                var fgSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sn in serialList)
                {
                    if (componentRows.Any(r =>
                            string.Equals((string)r.serial_code, sn, StringComparison.OrdinalIgnoreCase) &&
                            ComponentListHasSerial((string)r.component_list, sn)))
                        fgSet.Add(sn);
                }

                // Query 4: Pallet — lấy tất cả Serial trong SVN_Astro_Label_Data (toast), khớp trong C#
                var palletSerials = (await conn.QueryAsync<string>(@"
                    SELECT Serial FROM SVN_Astro_Label_Data
                    WHERE isDeleted = 0 AND EmployeeID = 'toast'
                      AND Serial IS NOT NULL",
                    commandTimeout: 30
                )).ToList();

                var palletSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sn in serialList)
                {
                    if (palletSerials.Any(ps => ps.Contains(sn, StringComparison.OrdinalIgnoreCase)))
                        palletSet.Add(sn);
                }

                // Query thêm: serial sai định dạng trong production logs (chưa có FCT)
                var existingSnSet = new HashSet<string>(serialList, StringComparer.OrdinalIgnoreCase);
                var wrongFmtProdRows = (await conn.QueryAsync(@"
                    SELECT serial_code AS SerialCode,
                           MAX(wo_code) AS WoCode,
                           MAX(CASE WHEN component_list IS NULL
                                      OR (component_list NOT LIKE '%""lotNumber"":""' + serial_code + '""%'
                                          AND component_list NOT LIKE '%""lotNumber"": ""' + serial_code + '""%')
                                    THEN 1 ELSE 0 END) AS HasWip,
                           MAX(CASE WHEN component_list IS NOT NULL
                                     AND (component_list LIKE '%""lotNumber"":""' + serial_code + '""%'
                                       OR component_list LIKE '%""lotNumber"": ""' + serial_code + '""%')
                                    THEN 1 ELSE 0 END) AS HasFg
                    FROM SVN_ProductionInputLogs
                    WHERE date_finished BETWEEN @from AND @to
                      AND serial_code IS NOT NULL
                      AND LTRIM(RTRIM(serial_code)) <> ''
                      AND LEN(LTRIM(RTRIM(serial_code))) <> 13
                    GROUP BY serial_code",
                    new { from, to },
                    commandTimeout: 30
                )).Cast<dynamic>().ToList();

                // Tổng hợp và phát hiện vi phạm thứ tự
                var alerts = serials
                    .Select(r => {
                        var sn      = (string)r.SerialNumber;
                        bool hasWip = wipSet.Contains(sn);
                        bool hasFct = !string.IsNullOrEmpty((string?)r.FctStatus);
                        bool hasFqc = !string.IsNullOrEmpty((string?)r.FqcStatus);
                        bool hasFg  = fgSet.Contains(sn);
                        bool hasPal = palletSet.Contains(sn);

                        var viols = new List<string>();
                        if (sn.Length != 13)                 viols.Add("SN ≠ 13 ký tự");
                        if (!hasWip && hasFct)               viols.Add("WIP");
                        if (!hasFqc && (hasFg || hasPal))    viols.Add("FQC");
                        if (!hasFg  && hasPal)               viols.Add("FG");

                        return new {
                            serialNumber = sn,
                            workOrder    = (string?)r.WorkOrder,
                            fctStatus    = (string?)r.FctStatus,
                            fctDatetime  = (DateTime?)r.FctDatetime,
                            fqcStatus    = (string?)r.FqcStatus,
                            hasWip, hasFqc, hasFg,
                            hasPallet    = hasPal,
                            violations   = viols
                        };
                    })
                    .Where(r => r.violations.Count > 0)
                    .ToList<object>();

                // Thêm serial sai định dạng từ production logs (chưa có trong danh sách FCT)
                foreach (var row in wrongFmtProdRows)
                {
                    var sn     = ((string)row.SerialCode).Trim();
                    if (existingSnSet.Contains(sn)) continue;
                    bool wfWip = (int)row.HasWip == 1;
                    bool wfFg  = (int)row.HasFg  == 1;
                    var viols  = new List<string>();
                    if (wfWip) viols.Add("SN sai · WIP");
                    if (wfFg)  viols.Add("SN sai · FG");
                    if (!wfWip && !wfFg) viols.Add("SN ≠ 13 ký tự");
                    alerts.Add(new {
                        serialNumber = sn,
                        workOrder    = (string?)row.WoCode,
                        fctStatus    = (string?)null,
                        fctDatetime  = (DateTime?)null,
                        fqcStatus    = (string?)null,
                        hasWip       = wfWip,
                        hasFqc       = false,
                        hasFg        = wfFg,
                        hasPallet    = palletSet.Contains(sn),
                        violations   = viols
                    });
                }

                var fgPending = serials
                    .Where(r => {
                        var sn = (string)r.SerialNumber;
                        return wipSet.Contains(sn) &&
                               !string.IsNullOrEmpty((string?)r.FctStatus) &&
                               !string.IsNullOrEmpty((string?)r.FqcStatus) &&
                               fgSet.Contains(sn) &&
                               !palletSet.Contains(sn);
                    })
                    .Select(r => {
                        var sn = (string)r.SerialNumber;
                        var wo = (string?)r.WorkOrder;
                        if (string.IsNullOrEmpty(wo)) wipWoMap.TryGetValue(sn, out wo);
                        return new {
                            serialNumber = sn,
                            workOrder    = wo,
                            fctStatus    = (string?)r.FctStatus,
                            fctDatetime  = (DateTime?)r.FctDatetime
                        };
                    })
                    .ToList<object>();

                int wrongFmtExtra = wrongFmtProdRows.Count(r => !existingSnSet.Contains(((string)r.SerialCode).Trim()));
                // Tổng Serial qua WIP = distinct serial có bản ghi WIP trong production logs
                int total       = wipSet.Count
                                + wrongFmtProdRows.Count(r =>
                                    !existingSnSet.Contains(((string)r.SerialCode).Trim()) &&
                                    (int)r.HasWip == 1);
                int totalFg     = fgSet.Count
                                + wrongFmtProdRows.Count(r =>
                                    !existingSnSet.Contains(((string)r.SerialCode).Trim()) &&
                                    (int)r.HasFg == 1);
                int missingWip  = serials.Count(r => !wipSet.Contains((string)r.SerialNumber));
                int missingFg   = serials.Count(r =>
                    !fgSet.Contains((string)r.SerialNumber) &&
                    palletSet.Contains((string)r.SerialNumber));
                int anyViol     = alerts.Count;
                int fgNotPacked = fgPending.Count;
                int wrongFormat = serials.Count(r => ((string)r.SerialNumber).Length != 13) + wrongFmtExtra;
                int complete    = serials.Count(r => {
                    var sn = (string)r.SerialNumber;
                    return wipSet.Contains(sn) &&
                           !string.IsNullOrEmpty((string?)r.FctStatus) &&
                           !string.IsNullOrEmpty((string?)r.FqcStatus) &&
                           fgSet.Contains(sn) &&
                           palletSet.Contains(sn);
                });

                return Ok(new {
                    ok   = true,
                    summary = new { total, totalFg, complete, missingWip, missingFg, anyViol, fgNotPacked, wrongFormat },
                    alerts,
                    fgPending
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = ex.Message });
            }
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

            // Exact serial match → hiện detail luôn, bỏ qua list và no-SN records
            var exact = summaries.FirstOrDefault(s =>
                string.Equals((string)s.serial_number, q, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
                return await GetToastSerialDetail((string)exact.serial_number, conn);

            // Tìm thêm bản ghi WIP không có SN nhưng component_list chứa query (cùng LOT)
            var noSnWip = (await conn.QueryAsync(
                @"SELECT id, wo_code, master_wo_code, date_finished, component_list
                  FROM SVN_ProductionInputLogs
                  WHERE ISNULL(component_list,'') LIKE '%' + @q + '%'
                    AND (serial_code IS NULL OR LTRIM(RTRIM(serial_code)) = '')",
                new { q })).Cast<dynamic>().ToList();

            if (!summaries.Any() && !noSnWip.Any())
                return Ok(new { ok = true, found = false });

            // Đúng 1 kết quả và không có no-SN records → hiện detail
            if (summaries.Count == 1 && !noSnWip.Any())
            {
                var only = summaries[0];
                bool isDirect = string.Equals((string)only.serial_number, q, StringComparison.OrdinalIgnoreCase);
                return await GetToastSerialDetail((string)only.serial_number, conn,
                    isDirect ? null : (string?)only.match_types,
                    isDirect ? null : q);
            }

            // Nhiều kết quả hoặc có no-SN records → list mode
            var noSnRows = noSnWip.Select(r => (object)new
            {
                serial_number = (string?)null,
                FCT_status    = (string?)null,
                FQC_status    = (string?)null,
                work_order    = (string?)r.wo_code,
                PalletID      = (string?)null,
                lots_raw      = (string?)r.component_list,
                match_types   = "Lot (Trạm WIP - không có SN)",
                renamed_to    = (string?)null,
                no_sn         = true,
                wip_id        = (int)r.id
            }).ToList();

            var allRows = new List<object>();
            allRows.AddRange(summaries.Cast<object>());
            allRows.AddRange(noSnRows);

            return Ok(new { ok = true, found = true, mode = "list", serials = allRows });
        }

        [HttpGet("api/ToastLookup/detail")]
        public async Task<IActionResult> GetToastLookupDetail([FromQuery] string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
                return BadRequest(new { ok = false, message = "Serial rong." });

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            return await GetToastSerialDetail(serial.Trim().ToUpper(), conn);
        }

        private static bool ComponentListHasSerial(string? json, string serial)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.ValueKind == JsonValueKind.Array &&
                       doc.RootElement.EnumerateArray().Any(e =>
                           e.TryGetProperty("lotNumber", out var lot) &&
                           string.Equals(lot.GetString(), serial, StringComparison.OrdinalIgnoreCase));
            }
            catch { return false; }
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

            // FG = serial xuất hiện làm lotNumber (exact match) trong component_list
            var fg  = prodRows.FirstOrDefault(x =>  ComponentListHasSerial((string?)x.component_list, serial));
            // WIP = serial không có trong component_list, ưu tiên state="Consumed"
            var wip = prodRows.FirstOrDefault(x => !ComponentListHasSerial((string?)x.component_list, serial)
                                                && string.Equals((string?)x.state, "Consumed", StringComparison.OrdinalIgnoreCase));

            // Fallback FG: trường hợp WSS đã bị sửa nên serial không còn trong component_list
            // Chỉ dùng state="Used" làm FG khi: đã có WIP riêng (2 records) HOẶC component_list rỗng
            if (fg == null)
            {
                var candidateFg = prodRows.FirstOrDefault(x =>
                    !ComponentListHasSerial((string?)x.component_list, serial) &&
                    string.Equals((string?)x.state, "Used", StringComparison.OrdinalIgnoreCase));
                if (candidateFg != null)
                {
                    var cl = ((string?)candidateFg.component_list ?? "").Trim();
                    bool clEmpty = string.IsNullOrEmpty(cl) || cl == "[]" || cl == "null";
                    if (wip != null || clEmpty)
                        fg = candidateFg;
                }
            }

            // Fallback WIP: nếu không có state="Consumed", lấy record còn lại không phải FG
            if (wip == null)
                wip = prodRows.FirstOrDefault(x => x != fg && !ComponentListHasSerial((string?)x.component_list, serial));

            // Fallback FG mở rộng: tìm record của serial KHÁC có serial này làm component (WSS)
            // Ví dụ: "0006-0-0926" có [WSS03-00290]=A101269182808 trong component_list
            if (fg == null)
            {
                var consumedRow = await Dapper.SqlMapper.QuerySingleOrDefaultAsync(conn,
                    @"SELECT TOP 1 id, state, wo_code, master_wo_code, date_finished,
                             status, component_list, consumed_wo_code
                      FROM SVN_ProductionInputLogs
                      WHERE (component_list LIKE '%""lotNumber"":""' + @serial + '""%'
                          OR component_list LIKE '%""lotNumber"": ""' + @serial + '""%')
                      ORDER BY date_finished DESC",
                    new { serial });
                if (consumedRow != null)
                    fg = consumedRow;
            }

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
            try
            {
                return await ReplaceToastComponentCore(req);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, message = "Lỗi server: " + ex.Message });
            }
        }

        private async Task<IActionResult> ReplaceToastComponentCore(ReplaceComponentRequest req)
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

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();

            // Lấy tất cả records rồi phân loại WIP/FG bằng component_list (giống lookup)
            var allLogs = (await Dapper.SqlMapper.QueryAsync(conn,
                "SELECT id, component_list, state, API_parameters FROM SVN_ProductionInputLogs WHERE serial_code = @serial ORDER BY date_finished DESC",
                new { serial })).Cast<dynamic>().ToList();

            dynamic? log;
            if (station == "WIP")
            {
                log = allLogs.FirstOrDefault(r => !ComponentListHasSerial((string?)r.component_list, serial)
                                               && string.Equals((string?)r.state, "Consumed", StringComparison.OrdinalIgnoreCase));
                log ??= allLogs.FirstOrDefault(r => !ComponentListHasSerial((string?)r.component_list, serial));
            }
            else
            {
                log = allLogs.FirstOrDefault(r => ComponentListHasSerial((string?)r.component_list, serial));
                // Fallback FG: WSS đã bị sửa nên serial không còn trong component_list
                // Chỉ dùng state="Used" khi đã có WIP riêng biệt (2 records) HOẶC component_list rỗng
                if (log == null)
                {
                    var wipLog = allLogs.FirstOrDefault(r => !ComponentListHasSerial((string?)r.component_list, serial)
                                                          && string.Equals((string?)r.state, "Consumed", StringComparison.OrdinalIgnoreCase));
                    var candidateFg = allLogs.FirstOrDefault(r =>
                        !ComponentListHasSerial((string?)r.component_list, serial) &&
                        string.Equals((string?)r.state, "Used", StringComparison.OrdinalIgnoreCase));
                    if (candidateFg != null)
                    {
                        var cl = ((string?)candidateFg.component_list ?? "").Trim();
                        bool clEmpty = string.IsNullOrEmpty(cl) || cl == "[]" || cl == "null";
                        if (wipLog != null || clEmpty)
                            log = candidateFg;
                    }
                }
            }

            if (log == null)
                return NotFound(new { ok = false, message = $"Không tìm thấy bản ghi {station} cho serial {serial}." });

            string? oldJson = (string?)log.component_list;
            if (string.IsNullOrWhiteSpace(oldJson))
                return BadRequest(new { ok = false, message = "Serial này chưa có danh sách linh kiện." });

            var productRow = await Dapper.SqlMapper.QuerySingleOrDefaultAsync(conn,
                "SELECT TOP 1 id FROM SVN_product_product WHERE default_code = @code",
                new { code = req.NewProductCode.Trim() });
            // Không bắt buộc phải tìm thấy product — nếu không có thì giữ nguyên product_id cũ
            int? lookedUpProductId = productRow != null ? (int?)((int)productRow.id) : null;

            JsonNode? node;
            try { node = JsonNode.Parse(oldJson); }
            catch { return BadRequest(new { ok = false, message = "Dữ liệu linh kiện hiện tại bị lỗi định dạng." }); }

            var arr = node as JsonArray;
            if (arr == null)
                return BadRequest(new { ok = false, message = "Dữ liệu linh kiện hiện tại không đúng định dạng." });

            JsonObject? target = null;
            int oldProductId = 0;
            foreach (var item in arr)
            {
                if (item is JsonObject obj &&
                    string.Equals((string?)obj["lotNumber"], req.OldLotNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    target = obj;
                    oldProductId = obj["product_id"]?.GetValue<int>() ?? 0;
                    break;
                }
            }
            if (target == null)
                return NotFound(new { ok = false, message = $"Không tìm thấy linh kiện với lot {req.OldLotNumber} trong trạm {station}." });

            int newProductId = lookedUpProductId ?? oldProductId;
            var oldLot = req.OldLotNumber.Trim().ToUpper();
            var newLot = req.NewLotNumber.Trim().ToUpper();

            target["product_id"] = newProductId;
            target["lotNumber"]  = newLot;
            string newJson = node!.ToJsonString();

            // Cập nhật API_parameters.LotScaneds: tìm theo product_id cũ, thay product_id và lotNumber mới
            string? oldApiJson = (string?)log.API_parameters;
            string? newApiJson = null;
            if (!string.IsNullOrWhiteSpace(oldApiJson) && oldProductId > 0)
            {
                try
                {
                    var apiNode = JsonNode.Parse(oldApiJson);
                    var lotScaneds = apiNode?["LotScaneds"] as JsonArray;
                    if (lotScaneds != null)
                    {
                        foreach (var item in lotScaneds)
                        {
                            if (item is JsonObject obj && (obj["product_id"]?.GetValue<int>() ?? 0) == oldProductId)
                            {
                                obj["product_id"] = newProductId;
                                obj["lotNumber"]  = newLot;
                                break;
                            }
                        }
                    }
                    newApiJson = apiNode?.ToJsonString();
                }
                catch { }
            }

            using var tran = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync(
                    @"UPDATE SVN_ProductionInputLogs
                      SET component_list = @newJson,
                          API_parameters = COALESCE(@newApiJson, API_parameters)
                      WHERE id = @id",
                    new { newJson, newApiJson, id = (int)log.id }, tran);

                tran.Commit();
            }
            catch (Exception ex)
            {
                try { tran.Rollback(); } catch { }
                return StatusCode(500, new { ok = false, message = "Lỗi khi lưu dữ liệu: " + ex.Message });
            }

            try
            {
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
            }
            catch { }

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

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();

            // Xác định đúng record cần cập nhật theo station
            var allLogsRn = (await conn.QueryAsync(
                "SELECT id, component_list, state FROM SVN_ProductionInputLogs WHERE serial_code = @serial",
                new { serial = oldSerial })).Cast<dynamic>().ToList();

            int? targetId = null;
            bool isFgRename = string.Equals(req.Station, "FG", StringComparison.OrdinalIgnoreCase);

            if (isFgRename)
            {
                var fgLog = allLogsRn.FirstOrDefault(r => ComponentListHasSerial((string?)r.component_list, oldSerial));
                if (fgLog == null)
                {
                    var wipLog2 = allLogsRn.FirstOrDefault(r => !ComponentListHasSerial((string?)r.component_list, oldSerial)
                                                              && string.Equals((string?)r.state, "Consumed", StringComparison.OrdinalIgnoreCase));
                    var cand = allLogsRn.FirstOrDefault(r => string.Equals((string?)r.state, "Used", StringComparison.OrdinalIgnoreCase));
                    if (cand != null)
                    {
                        var cl2 = ((string?)cand.component_list ?? "").Trim();
                        bool empty2 = string.IsNullOrEmpty(cl2) || cl2 == "[]" || cl2 == "null";
                        if (wipLog2 != null || empty2) fgLog = cand;
                    }
                }
                targetId = fgLog != null ? (int?)((int)fgLog.id) : null;
            }
            else // WIP
            {
                var wipLog = allLogsRn.FirstOrDefault(r => !ComponentListHasSerial((string?)r.component_list, oldSerial)
                                                        && string.Equals((string?)r.state, "Consumed", StringComparison.OrdinalIgnoreCase))
                          ?? allLogsRn.FirstOrDefault(r => !ComponentListHasSerial((string?)r.component_list, oldSerial));
                targetId = wipLog != null ? (int?)((int)wipLog.id) : null;
            }

            using var tran = conn.BeginTransaction();
            try
            {
                if (targetId.HasValue)
                {
                    // Chỉ cập nhật đúng record theo station
                    await conn.ExecuteAsync(
                        @"UPDATE SVN_ProductionInputLogs
                          SET serial_code = @newSerial,
                              API_parameters = CASE
                                  WHEN API_parameters IS NOT NULL AND ISJSON(API_parameters) = 1
                                  THEN REPLACE(
                                      JSON_MODIFY(API_parameters, '$.LotNumber', @newSerial),
                                      '""lotNumber"":""' + @oldSerial + '""',
                                      '""lotNumber"":""' + @newSerial + '""'
                                  )
                                  ELSE API_parameters
                              END
                          WHERE id = @id",
                        new { newSerial, oldSerial, id = targetId.Value }, tran);

                    // Với FG: cập nhật thêm component_list (WSS lot = serial cũ → serial mới)
                    if (isFgRename)
                        await conn.ExecuteAsync(
                            @"UPDATE SVN_ProductionInputLogs
                              SET component_list = REPLACE(component_list, '""' + @oldSerial + '""', '""' + @newSerial + '""')
                              WHERE id = @id AND component_list LIKE '%' + @oldSerial + '%'",
                            new { oldSerial, newSerial, id = targetId.Value }, tran);
                }
                else
                {
                    // Fallback: không tìm thấy record đúng station, báo lỗi
                    tran.Rollback();
                    return NotFound(new { ok = false, message = $"Không tìm thấy bản ghi {req.Station} cho serial {oldSerial}." });
                }

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
            catch (Exception ex)
            {
                try { tran.Rollback(); } catch { }
                return StatusCode(500, new { ok = false, message = "Lỗi khi lưu dữ liệu: " + ex.Message });
            }

            return Ok(new { ok = true, message = $"Đã chuyển dữ liệu sản xuất từ {oldSerial} sang {newSerial}." });
        }

        [HttpPost("api/ToastCorrection/assign-serial-to-wip")]
        public async Task<IActionResult> AssignSerialToWip([FromBody] AssignSerialToWipRequest req)
        {
            if (req.WipId <= 0 || string.IsNullOrWhiteSpace(req.Serial) ||
                string.IsNullOrWhiteSpace(req.SVNCode) || string.IsNullOrWhiteSpace(req.Reason))
                return BadRequest(new { ok = false, message = "Thiếu dữ liệu đầu vào." });

            var serial = req.Serial.Trim().ToUpper();

            using var conn = new System.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();

            var row = await conn.QueryFirstOrDefaultAsync(
                "SELECT id, wo_code FROM SVN_ProductionInputLogs WHERE id = @id AND (serial_code IS NULL OR LTRIM(RTRIM(serial_code)) = '')",
                new { id = req.WipId });
            if (row == null)
                return BadRequest(new { ok = false, message = "Bản ghi WIP không tồn tại hoặc đã có SN rồi." });

            await conn.ExecuteAsync(
                "UPDATE SVN_ProductionInputLogs SET serial_code = @serial WHERE id = @id",
                new { serial, id = req.WipId });

            await conn.ExecuteAsync(
                @"INSERT INTO SVN_Toast_Edit_Log (ActionType, SerialCode, RelatedSerial, Reason, EditedBy, EditedAt)
                  VALUES (@ActionType, @SerialCode, @RelatedSerial, @Reason, @EditedBy, @EditedAt)",
                new
                {
                    ActionType    = ToastEditActionType.AssignSerial,
                    SerialCode    = serial,
                    RelatedSerial = (string?)row.wo_code,
                    Reason        = req.Reason.Trim(),
                    EditedBy      = req.SVNCode.Trim(),
                    EditedAt      = DateTime.UtcNow.AddHours(7)
                });

            return Ok(new { ok = true, message = $"Đã gắn SN {serial} vào bản ghi WIP (WO: {row.wo_code})." });
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
