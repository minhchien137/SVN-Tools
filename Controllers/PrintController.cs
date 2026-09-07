using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace SVN_Tools.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PrintController : Controller
    {
        [HttpPost("printzpl")]
        public async Task<IActionResult> PrintZpl([FromBody] PrintRequest request)
        {
            if (string.IsNullOrEmpty(request.PrinterIpAddress) || string.IsNullOrEmpty(request.ZplCode))
            {
                return BadRequest("Địa chỉ IP máy in và mã ZPL không được để trống.");
            }

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(request.PrinterIpAddress, request.Port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] zplBytes = Encoding.UTF8.GetBytes(request.ZplCode);
                        for (int i = 0; i < request.Copies; i++)
                        {
                            await stream.WriteAsync(zplBytes, 0, zplBytes.Length);
                            Task.Delay(50).Wait();
                        }
                    }
                }
                return Ok($"Mã ZPL đã được gửi thành công đến máy in tại {request.PrinterIpAddress}:{request.Port}");
            }
            catch (SocketException se)
            {
                // Xử lý các lỗi kết nối cụ thể (ví dụ: máy in không trực tuyến, IP sai)
                return StatusCode(500, $"Lỗi kết nối đến máy in: {se.Message}. Vui lòng kiểm tra địa chỉ IP và đảm bảo máy in đang trực tuyến.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Đã xảy ra lỗi khi in: {ex.Message}");
            }
        }



        public class PrintRequest
        {
            public string PrinterIpAddress { get; set; }
            public int Port { get; set; } = 9100; // Cổng mặc định cho máy in Zebra
            public string ZplCode { get; set; }
            public int Copies { get; set; } = 1;
        }
    }
}
