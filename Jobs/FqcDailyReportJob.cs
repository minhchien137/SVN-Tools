using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SVN_Tools.Models.Label;

namespace SVN_Tools.Jobs
{
    [DisallowConcurrentExecution]
    public class FqcDailyReportJob : IJob
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FqcDailyReportJob> _logger;

        public FqcDailyReportJob(AppDbContext db, IHttpClientFactory httpClientFactory, ILogger<FqcDailyReportJob> logger)
        {
            _db = db;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var todayVN = DateTime.UtcNow.AddHours(7).Date;

            // 1. Lấy tất cả serial có FQC hôm nay
            var serials = await _db.SVNToastSerialInfos
                .AsNoTracking()
                .Where(x => x.FQCStatusDatetime.HasValue &&
                            x.FQCStatusDatetime.Value.Date == todayVN)
                .ToListAsync();

            if (!serials.Any())
            {
                _logger.LogInformation("FqcDailyReportJob: Không có serial FQC nào hôm nay.");
                return;
            }

            // 2. Lấy cookie
            var cookie = await _db.ProjectPasswordModels
                .Where(x => x.Password_name == "vindoo_cookie")
                .Select(x => x.Password_value)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(cookie))
            {
                _logger.LogError("FqcDailyReportJob: Không tìm thấy vindoo_cookie.");
                return;
            }

            // 3. Group theo WorkOrder
            var groups = serials
                .GroupBy(x => x.WorkOrder ?? "UNKNOWN")
                .ToList();

            foreach (var group in groups)
            {
                var woCode = group.Key;
                if (woCode == "UNKNOWN") continue;

                // 4. Tìm woPageId từ WO code
                int? woPageId = await GetWoPageId(woCode, cookie);
                if (!woPageId.HasValue)
                {
                    _logger.LogWarning($"FqcDailyReportJob: Không tìm thấy woPageId cho WO {woCode}");
                    continue;
                }

                // 5. Build bảng HTML
                var body = BuildHtmlTable(woCode, group.ToList(), todayVN);

                // 6. Gửi comment
                await PostComment(woPageId.Value, body, cookie);

                _logger.LogInformation($"FqcDailyReportJob: Đã gửi report cho WO {woCode} ({group.Count()} serial)");
            }
        }

        private async Task<int?> GetWoPageId(string woCode, string cookie)
        {
            var payload = new
            {
                id = 11,
                jsonrpc = "2.0",
                method = "call",
                @params = new
                {
                    model = "mrp.production",
                    method = "web_search_read",
                    args = Array.Empty<object>(),
                    kwargs = new
                    {
                        limit = 1,
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
                        count_limit = 1,
                        domain = new object[]
                        {
                            new object[] { "name", "=", woCode }
                        },
                        fields = new[] { "name" }
                    }
                }
            };

            try
            {
                using var req = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://sigmaworldwide.io/web/dataset/call_kw/mrp.production/web_search_read")
                {
                    Content = JsonContent.Create(payload)
                };
                req.Headers.Add("Cookie", cookie);

                var res = await _httpClient.SendAsync(req);
                res.EnsureSuccessStatusCode();

                using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
                var records = doc.RootElement
                    .GetProperty("result")
                    .GetProperty("records");

                if (records.GetArrayLength() == 0) return null;
                return records[0].GetProperty("id").GetInt32();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetWoPageId error: {ex.Message}");
                return null;
            }
        }

        private string BuildHtmlTable(string woCode, List<SVNToastSerialInfo> list, DateTime date)
        {
            var okCount = list.Count(x => x.FQCStatus?.ToUpper() == "OK");
            var ngCount = list.Count(x => x.FQCStatus?.ToUpper() == "NG");

            var sb = new StringBuilder();
            sb.Append($"<p><strong>Báo cáo FQC ngày {date:dd/MM/yyyy} — WO: {woCode}</strong></p>");
            sb.Append("<table style='width:100%;border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;'>");

            // Header
            sb.Append("<thead style='background-color:#714B67;color:white;'><tr>");
            sb.Append("<th style='padding:10px;border:1px solid #dee2e6;text-align:left;'>#</th>");
            sb.Append("<th style='padding:10px;border:1px solid #dee2e6;text-align:left;'>Serial Number</th>");
            sb.Append("<th style='padding:10px;border:1px solid #dee2e6;text-align:center;'>FCT</th>");
            sb.Append("<th style='padding:10px;border:1px solid #dee2e6;text-align:center;'>Thời gian FCT</th>");
            sb.Append("<th style='padding:10px;border:1px solid #dee2e6;text-align:center;'>FQC</th>");
            sb.Append("<th style='padding:10px;border:1px solid #dee2e6;text-align:center;'>Thời gian FQC</th>");
            sb.Append("</tr></thead><tbody>");

            int i = 1;
            foreach (var s in list.OrderBy(x => x.FQCStatusDatetime))
            {
                var bg = i % 2 == 0 ? "#f8f9fa" : "white";
                var fctColor = s.FCTStatus?.ToUpper() == "OK" ? "#28a745" : "#dc3545";
                var fqcColor = s.FQCStatus?.ToUpper() == "OK" ? "#28a745" : "#dc3545";

                sb.Append($"<tr style='background-color:{bg};'>");
                sb.Append($"<td style='padding:8px;border:1px solid #dee2e6;text-align:center;'>{i++}</td>");
                sb.Append($"<td style='padding:8px;border:1px solid #dee2e6;font-family:monospace;'>{s.SerialNumber}</td>");
                sb.Append($"<td style='padding:8px;border:1px solid #dee2e6;text-align:center;color:{fctColor};font-weight:bold;'>{s.FCTStatus}</td>");
                sb.Append($"<td style='padding:8px;border:1px solid #dee2e6;text-align:center;'>{s.FCTStatusDatetime:dd/MM/yyyy HH:mm:ss}</td>");
                sb.Append($"<td style='padding:8px;border:1px solid #dee2e6;text-align:center;color:{fqcColor};font-weight:bold;'>{s.FQCStatus}</td>");
                sb.Append($"<td style='padding:8px;border:1px solid #dee2e6;text-align:center;'>{s.FQCStatusDatetime:dd/MM/yyyy HH:mm:ss}</td>");
                sb.Append("</tr>");
            }

            // Summary
            sb.Append("<tr style='background-color:#eee;font-weight:bold;'>");
            sb.Append($"<td colspan='4' style='padding:10px;border:1px solid #dee2e6;'>TỔNG: {list.Count} serial</td>");
            sb.Append($"<td style='padding:10px;border:1px solid #dee2e6;text-align:center;color:#28a745;'>OK: {okCount}</td>");
            sb.Append($"<td style='padding:10px;border:1px solid #dee2e6;text-align:center;color:#dc3545;'>NG: {ngCount}</td>");
            sb.Append("</tr>");

            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private async Task PostComment(int threadId, string body, string cookie)
        {
            var payload = new
            {
                id = 43,
                jsonrpc = "2.0",
                method = "call",
                @params = new
                {
                    post_data = new
                    {
                        attachment_ids = Array.Empty<int>(),
                        attachment_tokens = Array.Empty<string>(),
                        body,
                        message_type = "comment",
                        partner_ids = Array.Empty<int>(),
                        canned_response_ids = Array.Empty<int>(),
                        subtype_xmlid = "mail.mt_comment"
                    },
                    thread_id = threadId,
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

            using var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://sigmaworldwide.io/mail/message/post")
            {
                Content = JsonContent.Create(payload)
            };
            req.Headers.Add("Cookie", cookie);

            var res = await _httpClient.SendAsync(req);
            _logger.LogInformation($"PostComment → {threadId}: {res.StatusCode}");
        }
    }
}