using System.Net.Http.Headers;
using System.Net.Http.Json;          // <-- thêm dòng này
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SVN_Tools.Models.Utils;

[ApiController]
[Route("api/[controller]")]
public class OdooController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private const string OdooApiUrl = "https://sigmaworldwide.io/web/dataset/call_kw/mrp.production/web_search_read";
    private const string GetInfoFromSO = "https://sigmaworldwide.io/web/dataset/call_kw/sale.order/read";
    private const string OdooApiUrl_2 = "https://sigmaworldwide.io/web/dataset/call_kw/mrp.production.progress/web_search_read";
    private const string SaleOrderLineUrl = "https://sigmaworldwide.io/web/dataset/call_kw/sale.order.line/read";

    private const string employeeOdoo = "https://sigmaworldwide.io/web/dataset/call_kw/hr.employee/web_search_read";

    private const string OdooPostCommentUrl = "https://sigmaworldwide.io/mail/message/post";

    private readonly AppDbContext _db;




    public OdooController(HttpClient httpClient, AppDbContext db)
    {
        _httpClient = httpClient;
        _db = db;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string productionCode)
    {
        // Tạo body JSON bằng chuỗi nội suy ($@"") như bạn muốn
        string finalJson = $@"
        {{
            ""id"": 555555555,
            ""jsonrpc"": ""2.0"",
            ""method"": ""call"",
            ""params"": {{
                ""model"": ""mrp.production"",
                ""method"": ""web_search_read"",
                ""args"": [],
                ""kwargs"": {{
                    ""limit"": 80,
                    ""offset"": 0,
                    ""order"": """",
                    ""context"": {{
                        ""lang"": ""vi_VN"",
                        ""tz"": ""Asia/Ho_Chi_Minh"",
                        ""uid"": 2,
                        ""allowed_company_ids"": [1],
                        ""bin_size"": true,
                        ""default_company_id"": 1
                    }},
                    ""count_limit"": 10001,
                    ""domain"": [
                        ""&"",
                        [""picking_type_id.active"", ""="", true],
                        ""&"",
                        [""state"", ""in"", [""draft"", ""confirmed"", ""progress"", ""to_close""]],
                        ""|"",
                        [""name"", ""ilike"", ""{productionCode}""],
                        [""origin"", ""ilike"", ""xxxxxxxxx""]
                    ],
                    ""fields"": [
                        ""activity_exception_decoration"", ""activity_exception_icon"", ""activity_state"",
                        ""activity_summary"", ""activity_type_icon"", ""activity_type_id"",
                        ""company_id"", ""product_uom_category_id"", ""priority"", ""message_needaction"",
                        ""name"", ""date_planned_start"", ""date_deadline"", ""product_id"",
                        ""lot_producing_id"", ""bom_id"", ""activity_ids"", ""origin"", ""user_id"",
                        ""components_availability_state"", ""components_availability"",
                        ""reservation_state"", ""product_qty"", ""product_uom_id"",
                        ""production_duration_expected"", ""production_real_duration"",
                        ""progress"", ""state"", ""delay_alert_date"", ""json_popover""
                    ]
                }}
            }}
        }}";

        var jsonContent = new StringContent(finalJson, Encoding.UTF8, "application/json");

        try
        {
            // Tạo request riêng để thêm cookie cho request này thôi
            using var request = new HttpRequestMessage(HttpMethod.Post, OdooApiUrl)
            {
                Content = jsonContent
            };

            // Thêm cookie cho request này
            var cookie = await _db.ProjectPasswordModels
            .Where(x => x.Password_name == "vindoo_cookie")
            .Select(x => x.Password_value)
            .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(cookie))
            {
                return StatusCode(500, new { message = "vindoo_cookie not found in database" });
            }
            request.Headers.Add("Cookie", cookie);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            return Ok(responseBody);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { message = "Error calling Odoo API", details = ex.Message });
        }
    }


    [HttpGet("search2")]
    public async Task<IActionResult> Search2([FromQuery] string productionCode)
    {

        var finalJson = $@"
{{
    ""id"": 11,
    ""jsonrpc"": ""2.0"",
    ""method"": ""call"",
    ""params"": {{
        ""model"": ""mrp.production.progress"",
        ""method"": ""web_search_read"",
        ""args"": [],
        ""kwargs"": {{
            ""limit"": 80,
            ""offset"": 0,
            ""order"": """",
            ""context"": {{
                ""lang"": ""vi_VN"",
                ""tz"": ""Asia/Ho_Chi_Minh"",
                ""uid"": 2,
                ""allowed_company_ids"": [1],
                ""bin_size"": true,
                ""params"": {{
                    ""action"": 1090,
                    ""model"": ""mrp.production.progress"",
                    ""view_type"": ""list"",
                    ""cids"": 1,
                    ""menu_id"": 248
                }},
                ""default_company_id"": 1
            }},
            ""count_limit"": 10001,
            ""domain"": [
                [""name"", ""ilike"", ""{productionCode}""]
            ],
            ""fields"": [
                ""company_id"",
                ""priority"",
                ""name"",
                ""product_id"",
                ""date_planned_start"",
                ""date_planned_finished"",
                ""date_start"",
                ""date_finished"",
                ""date_deadline"",
                ""product_qty"",
                ""qty_produced"",
                ""qty_remaining"",
                ""progress"",
                ""product_uom_id"",
                ""state""
            ]
        }}
    }}
}}";

        var jsonContent = new StringContent(finalJson, Encoding.UTF8, "application/json");

        try
        {
            // Tạo request riêng để thêm cookie cho request này thôi
            using var request = new HttpRequestMessage(HttpMethod.Post, OdooApiUrl_2)
            {
                Content = jsonContent
            };

            // Thêm cookie cho request này
            var cookie = await _db.ProjectPasswordModels
            .Where(x => x.Password_name == "vindoo_cookie")
            .Select(x => x.Password_value)
            .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(cookie))
            {
                return StatusCode(500, new { message = "vindoo_cookie not found in database" });
            }
            request.Headers.Add("Cookie", cookie);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            return Ok(responseBody);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { message = "Error calling Odoo API", details = ex.Message });
        }
    }

    [HttpGet("getPONumber")]
    public async Task<IActionResult> GetPONumber([FromQuery] string productionCode)
    {
        // 1) Tạo payload dạng object (không build chuỗi JSON)
        var payload1 = new
        {
            id = 98,
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                model = "mrp.production",
                method = "web_search_read",
                args = Array.Empty<object>(),
                kwargs = new
                {
                    limit = 80,
                    offset = 0,
                    order = "",
                    context = new
                    {
                        lang = "vi_VN",
                        tz = "Asia/Ho_Chi_Minh",
                        uid = 2,
                        allowed_company_ids = new[] { 1 },
                        bin_size = true,
                        default_company_id = 1
                    },
                    count_limit = 10001,
                    domain = new object[]
                {
                        "&",
                        new object[] { "picking_type_id.active", "=", true },
                        "|",
                        new object[] { "name", "ilike", productionCode },     // ví dụ: "NM/MO/01415"
                        new object[] { "origin", "ilike", "xxxxxxxxxxxx" }
                },
                    // lấy đúng 3 field như yêu cầu
                    fields = new[] { "name", "product_id", "origin" }
                }
            }
        };

        try
        {
            // 2) Gửi request #1
            using var request1 = new HttpRequestMessage(HttpMethod.Post, OdooApiUrl)
            {
                Content = JsonContent.Create(payload1)
            };
            var cookie = await _db.ProjectPasswordModels
            .Where(x => x.Password_name == "vindoo_cookie")
            .Select(x => x.Password_value)
            .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(cookie))
            {
                return StatusCode(500, new { message = "vindoo_cookie not found in database" });
            }
            request1.Headers.Add("Cookie", cookie);

            var response = await _httpClient.SendAsync(request1);
            response.EnsureSuccessStatusCode();

            // Parse JSON
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            // ---- Lấy records[0].origin và records[0].product_id ----
            string? origin = null;
            string? sku = null;

            try
            {
                var first = doc.RootElement
                    .GetProperty("result")
                    .GetProperty("records")[0];

                // origin: ví dụ "S00271"
                origin = first.GetProperty("origin").GetString();

                // product_id: dạng [1179, "[810114362556] 955-..."]
                // lấy element thứ 2 (index 1) rồi tách SKU trong ngoặc vuông
                if (first.TryGetProperty("product_id", out var pid) && pid.ValueKind == JsonValueKind.Array && pid.GetArrayLength() >= 2)
                {
                    var nameWithSku = pid[1].GetString(); // "[810114362556] 955-..."
                    if (!string.IsNullOrEmpty(nameWithSku))
                    {
                        var m = Regex.Match(nameWithSku, @"\[(.*?)\]");
                        if (m.Success) sku = m.Groups[1].Value; // ==> "810114362556"
                    }
                }
            }
            catch
            {
                origin = null;
                sku = null;
            }

            if (origin == null) return NotFound();
            int so = int.Parse(origin.Substring(1));

            // Part2 - New Implementation
            var payload2 = new
            {
                id = 6,
                jsonrpc = "2.0",
                method = "call",
                @params = new
                {
                    args = new object[]
                    {
                new object[] { so },
                new object[] { "x_ERP_PO", "x_ERP_Deadline_Shipment", "order_line" }
                    },
                    model = "sale.order",
                    method = "read",
                    kwargs = new
                    {
                        context = new
                        {
                            lang = "vi_VN",
                            tz = "Asia/Ho_Chi_Minh",
                            uid = 2,
                            allowed_company_ids = new[] { 1 },
                            bin_size = true,
                            @params = new
                            {
                                id = so,
                                cids = 1,
                                menu_id = 302,
                                action = 500,
                                model = "sale.order",
                                view_type = "form"
                            }
                        }
                    }
                }
            };

            using var request2 = new HttpRequestMessage(HttpMethod.Post, GetInfoFromSO)
            {
                Content = JsonContent.Create(payload2)
            };
            request2.Headers.Add("Cookie", cookie);

            var response2 = await _httpClient.SendAsync(request2);
            response2.EnsureSuccessStatusCode();

            using var doc2 = await JsonDocument.ParseAsync(await response2.Content.ReadAsStreamAsync());

            // An toàn: kiểm tra result có dạng mảng và có phần tử
            if (!doc2.RootElement.TryGetProperty("result", out var resultElem)
                || resultElem.ValueKind != JsonValueKind.Array
                || resultElem.GetArrayLength() == 0)
            {
                return Ok(new { poNumbers = Array.Empty<string>(), deadlines = Array.Empty<string>(), orderLine = Array.Empty<int>() });
            }

            var second = resultElem[0];

            // Helper local: đọc string nếu là string, nếu là false/null thì trả "".
            static string ReadOdooStringOrEmpty(JsonElement parent, string propName)
            {
                if (parent.TryGetProperty(propName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String) return prop.GetString() ?? "";
                    // Odoo có thể trả false cho field rỗng
                    if (prop.ValueKind == JsonValueKind.False || prop.ValueKind == JsonValueKind.Null) return "";
                }
                return "";
            }

            var xERPPO = ReadOdooStringOrEmpty(second, "x_ERP_PO");
            var xERPDeadline = ReadOdooStringOrEmpty(second, "x_ERP_Deadline_Shipment");

            // Tách thành mảng, bỏ phần tử rỗng
            var poNumbers = xERPPO.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToArray();

            var deadlines = xERPDeadline.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s.Trim())
                                        .ToArray();

            // order_line: có thể là mảng hoặc false
            int[] orderLine = Array.Empty<int>();
            if (second.TryGetProperty("order_line", out var ol))
            {
                if (ol.ValueKind == JsonValueKind.Array)
                {
                    orderLine = ol.EnumerateArray()
                                .Where(e => e.ValueKind == JsonValueKind.Number)
                                .Select(e => e.GetInt32())
                                .ToArray();
                }
                // nếu là False/Null thì giữ mảng rỗng
            }

            // Part 3
            string[] names = Array.Empty<string>();
            if (orderLine.Length > 0)
            {
                var payload3 = new
                {
                    id = 7,
                    jsonrpc = "2.0",
                    method = "call",
                    @params = new
                    {
                        args = new object[]
                        {
                orderLine, // chính là mảng order_line từ Part2
                new object[]
                {
                    "sequence","display_type","product_uom_category_id","product_type",
                    "product_updatable","product_id","product_template_id",
                    "product_template_attribute_value_ids","product_custom_attribute_value_ids",
                    "product_no_variant_attribute_value_ids","is_configurable_product","name",
                    "product_license_version_ids","route_id","product_uom_qty","qty_delivered",
                    "virtual_available_at_date","qty_available_today","free_qty_today","scheduled_date",
                    "forecast_expected_date","warehouse_id","move_ids","qty_to_deliver","is_mto",
                    "display_qty_widget","qty_delivered_method","qty_invoiced","qty_to_invoice",
                    "product_uom_readonly","product_uom","customer_lead","product_packaging_qty",
                    "product_packaging_id","price_unit","tax_id","is_downpayment","price_subtotal",
                    "state","invoice_status","currency_id","price_tax","company_id"
                }
                        },
                        model = "sale.order.line",
                        method = "read",
                        kwargs = new
                        {
                            context = new
                            {
                                lang = "vi_VN",
                                tz = "Asia/Ho_Chi_Minh",
                                uid = 2,
                                allowed_company_ids = new[] { 1 },
                                @params = new
                                {
                                    id = so, // id sale.order hiện tại
                                    cids = 1,
                                    menu_id = 302,
                                    action = 500,
                                    model = "sale.order",
                                    view_type = "form"
                                }
                            }
                        }
                    }
                };

                using var request3 = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://sigmaworldwide.io/web/dataset/call_kw/sale.order.line/read"
                )
                {
                    Content = JsonContent.Create(payload3)
                };
                request3.Headers.Add("Cookie", cookie);

                var response3 = await _httpClient.SendAsync(request3);
                response3.EnsureSuccessStatusCode();

                using var doc3 = await JsonDocument.ParseAsync(await response3.Content.ReadAsStreamAsync());

                if (doc3.RootElement.TryGetProperty("result", out var lines)
    && lines.ValueKind == JsonValueKind.Array
    && lines.GetArrayLength() > 0)
                {
                    var list = new List<string>();
                    foreach (var line in lines.EnumerateArray())
                    {
                        if (line.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String)
                        {
                            var s = nm.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                // Dùng regex để lấy chuỗi trong ngoặc vuông
                                var match = Regex.Match(s, @"\[(\d+)\]");
                                if (match.Success)
                                {
                                    list.Add(match.Groups[1].Value); // chỉ lấy số SKU
                                }
                            }
                        }
                    }
                    names = list.ToArray();
                }
            }

            // Part4 - Tìm vị trí i của sku trong names và trả về poNumbers[i], deadlines[i]
            string? poNumberSingle = null;
            string? deadlineSingle = null;

            if (!string.IsNullOrWhiteSpace(sku) && names.Length > 0)
            {
                // tìm vị trí i khớp SKU
                int idx = Array.FindIndex(names, x => string.Equals(x, sku, StringComparison.OrdinalIgnoreCase));

                if (idx >= 0 && idx < poNumbers.Length && idx < deadlines.Length)
                {
                    poNumberSingle = poNumbers[idx];

                    // format deadline nếu ở dạng yyyymmdd, còn không thì giữ nguyên
                    var d = deadlines[idx];
                    if (!string.IsNullOrWhiteSpace(d) && d.Length == 8 && d.All(char.IsDigit))
                        deadlineSingle = $"{d.Substring(0, 4)}-{d.Substring(4, 2)}-{d.Substring(6, 2)}";
                    else
                        deadlineSingle = d;
                }
            }

            // Trả về CHỈ 2 field theo yêu cầu
            return Ok(new { poNumber = poNumberSingle, deadline = deadlineSingle });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(500, new { message = "Error calling Odoo API", details = ex.Message });
        }
    }

    [HttpGet("getworkorderfromserial/{serial}")]
    public async Task<IActionResult> GetWorkOrderFromSerial(string serial)
    {
        if (string.IsNullOrWhiteSpace(serial))
            return BadRequest(new { message = "Serial không được rỗng." });

        // === Step 1: Gọi stock.lot/web_search_read ===
        var payload1 = new
        {
            id = 18,
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                model = "stock.lot",
                method = "web_search_read",
                args = Array.Empty<object>(),
                kwargs = new
                {
                    limit = 80,
                    offset = 0,
                    order = "",
                    domain = new object[] {
                    new object[] { "name", "ilike", serial }
                },
                    fields = new[] { "name", "product_id" }
                }
            }
        };

        var cookie = await _db.ProjectPasswordModels
            .Where(x => x.Password_name == "vindoo_cookie")
            .Select(x => x.Password_value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(cookie))
            return StatusCode(500, new { message = "vindoo_cookie not found in database" });

        int lotId;
        try
        {
            using var req1 = new HttpRequestMessage(HttpMethod.Post, "https://sigmaworldwide.io/web/dataset/call_kw/stock.lot/web_search_read")
            {
                Content = JsonContent.Create(payload1)
            };
            req1.Headers.Add("Cookie", cookie);

            var res1 = await _httpClient.SendAsync(req1);
            res1.EnsureSuccessStatusCode();

            using var doc1 = await JsonDocument.ParseAsync(await res1.Content.ReadAsStreamAsync());
            var records = doc1.RootElement
                .GetProperty("result")
                .GetProperty("records");

            if (records.GetArrayLength() == 0)
                return NotFound(new { message = "Không tìm thấy serial trong stock.lot." });

            lotId = records[0].GetProperty("id").GetInt32();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi gọi stock.lot/web_search_read", details = ex.Message });
        }

        // === Step 2: Gọi stock.traceability.report/get_html ===
        var payload2 = new
        {
            id = 23,
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                args = new object[]
                {
                new
                {
                    lang = "vi_VN",
                    tz = "Asia/Ho_Chi_Minh",
                    uid = 2,
                    allowed_company_ids = new[] { 1 },
                    active_id = lotId,
                    model = "stock.lot",
                    ttype = false,
                    auto_unfold = false,
                    lot_name = false
                }
                },
                model = "stock.traceability.report",
                method = "get_html",
                kwargs = new
                {
                    context = new
                    {
                        lang = "vi_VN",
                        tz = "Asia/Ho_Chi_Minh",
                        uid = 2,
                        allowed_company_ids = new[] { 1 }
                    }
                }
            }
        };

        string? workOrder = null;
        string? activeId = null;
        try
        {
            using var req2 = new HttpRequestMessage(HttpMethod.Post, "https://sigmaworldwide.io/web/dataset/call_kw/stock.traceability.report/get_html")
            {
                Content = JsonContent.Create(payload2)
            };
            req2.Headers.Add("Cookie", cookie);

            var res2 = await _httpClient.SendAsync(req2);
            res2.EnsureSuccessStatusCode();

            using var doc2 = await JsonDocument.ParseAsync(await res2.Content.ReadAsStreamAsync());
            var html = doc2.RootElement
                .GetProperty("result")
                .GetProperty("html")
                .GetString() ?? "";

            // === Step 3: Regex để lấy chuỗi NM/MO/... ===
            // Tìm mã có dạng NM/MO/xxxxx hoặc tương tự
            var match = Regex.Match(html, @"data-active-id=""(\d+)""[^>]*?>\s*(NM/MO/\d+(?:-\d+)?)\s*<", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                activeId = match.Groups[1].Value;
                workOrder = match.Groups[2].Value;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi gọi stock.traceability.report/get_html", details = ex.Message });
        }

        if (string.IsNullOrEmpty(workOrder))
            return NotFound(new { message = "Không tìm thấy Work Order trong traceability report." });

        // === Step 4: Trả kết quả ===
        return Ok(new { serial, workOrder, woPageId = int.Parse(activeId), serialPageId = lotId });
    }


    [HttpPost("postcomment")]
    public async Task<IActionResult> PostCommentToWorkOrder([FromBody] OdooCommentRequest req)
    {
        if (req == null || req.ThreadId <= 0 || string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { ok = false, message = "Thiếu dữ liệu (threadId hoặc body)." });

        // === Lấy cookie từ DB ===
        var cookie = await _db.ProjectPasswordModels
            .Where(x => x.Password_name == "vindoo_cookie")
            .Select(x => x.Password_value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(cookie))
            return StatusCode(500, new { ok = false, message = "vindoo_cookie not found in database" });

        // === Payload gửi tới Odoo ===
        var payload = new
        {
            id = 13,
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                post_data = new
                {
                    attachment_ids = new int[] { },
                    attachment_tokens = new string[] { },
                    body = req.Body,
                    message_type = "comment",
                    partner_ids = new int[] { },
                    canned_response_ids = new int[] { },
                    subtype_xmlid = "mail.mt_comment"
                },
                thread_id = req.ThreadId,
                thread_model = "mrp.production",
                context = new
                {
                    mail_post_autofollow = true,
                    lang = "vi_VN",
                    tz = "Asia/Ho_Chi_Minh",
                    uid = 2,
                    allowed_company_ids = new[] { 1 }
                }
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OdooPostCommentUrl)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("Cookie", cookie);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { ok = false, message = "Odoo API error", details = content });

            return Ok(new { ok = true, message = "Đã gửi comment thành công.", response = content });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, message = "Lỗi khi gọi mail/message/post", details = ex.Message });
        }
    }

    public class OdooCommentRequest
    {
        public int? ThreadId { get; set; }
        public string Body { get; set; } = string.Empty;

        public string? WOcode { get; set; }
    }

    [HttpPost("postcommenttoWO")]
    public async Task<IActionResult> PostCommentToWorkOrderNew([FromBody] OdooCommentRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.WOcode) || string.IsNullOrWhiteSpace(req.Body))
            return BadRequest(new { ok = false, message = "Thiếu dữ liệu (WO hoặc body)." });

        // === Lấy cookie từ DB ===
        var cookie = await _db.ProjectPasswordModels
            .Where(x => x.Password_name == "vindoo_cookie")
            .Select(x => x.Password_value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(cookie))
            return StatusCode(500, new { ok = false, message = "vindoo_cookie not found in database" });

        var payload1 = new
        {
            id = 11,
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                model = "mrp.production",
                method = "web_search_read",
                args = new object[] { },
                kwargs = new
                {
                    limit = 80,
                    offset = 0,
                    order = "",
                    context = new
                    {
                        lang = "vi_VN",
                        tz = "Asia/Ho_Chi_Minh",
                        uid = 2,
                        allowed_company_ids = new[] { 1 },
                        bin_size = true,
                        @params = new
                        {
                            action = 431,
                            model = "mrp.production",
                            view_type = "list",
                            cids = 1,
                            menu_id = 248
                        },
                        default_company_id = 1
                    },
                    count_limit = 10001,
                    domain = new object[]
            {
                "&",
                new object[] {"picking_type_id.active", "=", true},
                "|",
                new object[] {"name", "ilike", req.WOcode},
                new object[] {"origin", "ilike", "xxxxxxxxxxxxx"}
            },
                    fields = new[]
            {
               "name","product_id","origin","product_qty",
            }
                }
            }
        };

        using var req1 = new HttpRequestMessage(HttpMethod.Post, OdooApiUrl)
        {
            Content = JsonContent.Create(payload1)
        };
        req1.Headers.Add("Cookie", cookie);

        var res1 = await _httpClient.SendAsync(req1);
        res1.EnsureSuccessStatusCode();

        using var doc1 = await JsonDocument.ParseAsync(await res1.Content.ReadAsStreamAsync());
        var records = doc1.RootElement
            .GetProperty("result")
            .GetProperty("records");

        var lotId = records[0].GetProperty("id").GetInt32();


        // === Payload gửi tới Odoo ===
        var payload2 = new
        {
            id = 13,
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                post_data = new
                {
                    attachment_ids = new int[] { },
                    attachment_tokens = new string[] { },
                    body = req.Body,
                    message_type = "comment",
                    partner_ids = new int[] { },
                    canned_response_ids = new int[] { },
                    subtype_xmlid = "mail.mt_comment"
                },
                thread_id = lotId,
                thread_model = "mrp.production",
                context = new
                {
                    mail_post_autofollow = true,
                    lang = "vi_VN",
                    tz = "Asia/Ho_Chi_Minh",
                    uid = 2,
                    allowed_company_ids = new[] { 1 }
                }
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, OdooPostCommentUrl)
            {
                Content = JsonContent.Create(payload2)
            };
            request.Headers.Add("Cookie", cookie);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, new { ok = false, message = "Odoo API error", details = content });

            return Ok(new { ok = true, message = "Đã gửi comment thành công.", response = content });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, message = "Lỗi khi gọi mail/message/post", details = ex.Message });
        }
    }







}