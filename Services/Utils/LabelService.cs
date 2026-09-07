using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Text;
using QRCoder;
using SVN_Tools.Models.Utils;






namespace SVN_Tools.Services.Utils
{
    public class LabelService
    {
        private readonly QRtoZPLService _qrService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;

        public LabelService(QRtoZPLService qrService, IHttpClientFactory httpClientFactory, HttpClient httpClient)
        {
            _qrService = qrService;
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClient;
        }

        public string GenerateTemplate(PrinterData printerData, LabelDataRequest request)
        {
            string result = printerData.ZPL_Temp;

            if (printerData.target == "Toast")
            {
                string[] parts = request.lotId.Split('-');
                string toastPartNumber1 = request.toastPartNumber.Substring(0, 2);
                string toastPartNumber2 = request.toastPartNumber.Substring(2);

                // string serialBlock1Zpl = GenerateSerialBlockToast(request.serialNumber1, "167,1019", "275,1069");
                // string serialBlock2Zpl = GenerateSerialBlockToast(request.serialNumber2, "167,1175", "275,1225");
                // string serialBlock3Zpl = GenerateSerialBlockToast(request.serialNumber3, "167,1331", "275,1380");
                // string serialBlock4Zpl = GenerateSerialBlockToast(request.serialNumber4, "167,1482", "275,1532");
                // string serialBlock5Zpl = GenerateSerialBlockToast(request.serialNumber5, "167,1638", "275,1688");
                string serialBlock1Zpl = GenerateSerialBlockToast(request.serialNumber1, "264,1043", "485,1077");
                string serialBlock2Zpl = GenerateSerialBlockToast(request.serialNumber2, "264,1193", "485,1227");
                string serialBlock3Zpl = GenerateSerialBlockToast(request.serialNumber3, "264,1343", "485,1377");
                string serialBlock4Zpl = GenerateSerialBlockToast(request.serialNumber4, "264,1493", "485,1527");
                string serialBlock5Zpl = GenerateSerialBlockToast(request.serialNumber5, "264,1643", "485,1677");
                result = result
                .Replace("{toastPartNumber}", request.toastPartNumber)
                .Replace("{toastPartNumber1}", toastPartNumber1)
                .Replace("{toastPartNumber2}", toastPartNumber2)
                .Replace("{modelNumber}", request.modelNumber)
                .Replace("{desc}", request.desc)
                .Replace("{descFr}", request.descFr)
                .Replace("{quantity}", request.quantity.ToString())
                .Replace("{serialBlock1}", serialBlock1Zpl)
                .Replace("{serialBlock2}", serialBlock2Zpl)
                .Replace("{serialBlock3}", serialBlock3Zpl)
                .Replace("{serialBlock4}", serialBlock4Zpl)
                .Replace("{serialBlock5}", serialBlock5Zpl)
                .Replace("{lotId}", request.lotId)
                .Replace("{poNumber}", request.poNumber)
                .Replace("{lotId1}", parts[0])
                .Replace("{lotId2}", parts[1])
                .Replace("{lotId3}", parts[2]);


            }
            else if (printerData.target == "Astro")
            {
                string serialBlock1Zpl = GenerateSerialBlockAstro(request.serialNumber1, "315,1254", "352,1254");
                string serialBlock2Zpl = GenerateSerialBlockAstro(request.serialNumber2, "432,1254", "469,1254");
                string serialBlock3Zpl = GenerateSerialBlockAstro(request.serialNumber3, "554,1254", "591,1254");
                string serialBlock4Zpl = GenerateSerialBlockAstro(request.serialNumber4, "671,1254", "708,1254");
                string serialBlock5Zpl = GenerateSerialBlockAstro(request.serialNumber5, "787,1254", "824,1254");
                string serialBlock6Zpl = GenerateSerialBlockAstro(request.serialNumber6, "904,1254", "941,1254");
                string serialBlock7Zpl = GenerateSerialBlockAstro(request.serialNumber7, "315,789", "352,789");
                string serialBlock8Zpl = GenerateSerialBlockAstro(request.serialNumber8, "432,789", "469,789");
                string serialBlock9Zpl = GenerateSerialBlockAstro(request.serialNumber9, "554,789", "591,789");
                string serialBlock10Zpl = GenerateSerialBlockAstro(request.serialNumber10, "671,789", "708,789");
                string serialBlock11Zpl = GenerateSerialBlockAstro(request.serialNumber11, "787,789", "824,789");
                string serialBlock12Zpl = GenerateSerialBlockAstro(request.serialNumber12, "904,789", "941,789");
                result = result.Replace("{desc}", request.desc ?? "N/A")
                .Replace("{dateCode}", request.dateCode ?? "xx/xx")
                .Replace("{quantity}", request.quantity.ToString() ?? "N/A")
                .Replace("{packageId}", request.packageId ?? "N/A")
                .Replace("{serialBlock1}", serialBlock1Zpl)
                .Replace("{serialBlock2}", serialBlock2Zpl)
                .Replace("{serialBlock3}", serialBlock3Zpl)
                .Replace("{serialBlock4}", serialBlock4Zpl)
                .Replace("{serialBlock5}", serialBlock5Zpl)
                .Replace("{serialBlock6}", serialBlock6Zpl)
                .Replace("{serialBlock7}", serialBlock7Zpl)
                .Replace("{serialBlock8}", serialBlock8Zpl)
                .Replace("{serialBlock9}", serialBlock9Zpl)
                .Replace("{serialBlock10}", serialBlock10Zpl)
                .Replace("{serialBlock11}", serialBlock11Zpl)
                .Replace("{serialBlock12}", serialBlock12Zpl)
                .Replace("{netWeight}", request.netWeight ?? "N/A")
                .Replace("{grossWeight}", request.grossWeight ?? "N/A");
            }
            else if (printerData.target == "Whoop" || printerData.target == "Whoopnew")
            {

                result = result.Replace("{poNumber}", request.poNumber ?? "N/A")
                .Replace("{upcItem}", request.upcItem ?? "xx/xx")
                .Replace("{upcPnNumber}", request.upcPnNumber ?? "N/A")
                .Replace("{itemSize}", request.itemSize ?? "N/A")
                .Replace("{upcColor}", request.upcColor ?? "N/A")
                .Replace("{quantity}", request.quantity.ToString() ?? "N/A")
                .Replace("{ctNumber}", request.ctNumber ?? "N/A")
                .Replace("{dateWhoop}", request.dateWhoop ?? "mm/dd/yyyy")
                .Replace("{upcNumber}", request.upcNumber ?? "N/A");
            }
            else if (printerData.target == "WalterInBox")
            {
                var LN = request.LN;
                string LN1 = LN.Substring(0, LN.Length - 1);
                string LN2 = LN.Substring(LN.Length - 1);
                string lotSuffix = LN.Substring(LN.Length - 5);
                string lotSuffix1 = lotSuffix.Substring(0, 1);
                string lotSuffix2 = lotSuffix.Substring(1);
                result = result.Replace("{upcNumber}", request.GTPN ?? "N/A")
                .Replace("{upcPnNumber}", request.VDPN ?? "N/A")
                .Replace("{LN}", request.LN ?? "N/A")
                .Replace("{DC}", request.DC ?? "N/A")
                .Replace("{LN1}", LN1 ?? "N/A")
                .Replace("{LN2}", LN2 ?? "N/A")
                .Replace("{lotSuffix}", lotSuffix ?? "N/A")
                .Replace("{lotSuffix1}", lotSuffix1 ?? "N/A")
                .Replace("{lotSuffix2}", lotSuffix2 ?? "N/A")
                .Replace("{quantity}", request.quantity.ToString());
            }
            return result;
        }

        public string GenerateSerialBlockToast(string serialNumber, string barcodeFtCoordinates, string textFtCoordinates)
        {
            if (string.IsNullOrEmpty(serialNumber))
            {
                return ""; // Nếu không có serial, trả về chuỗi rỗng
            }

            // Tạo chuỗi ZPL cho barcode và text serial
            // Lưu ý: Đảm bảo có ký tự xuống dòng (\n) nếu cần để giữ layout đúng

            return $@"
                    ^BY4,3,102^FT{barcodeFtCoordinates}^BCN,,N,N
                    ^FH\^FD>:{serialNumber}^FS
                    ^FT{textFtCoordinates}^A@N,33,34,TT0003M_^FH\^CI28^FD{serialNumber}^FS^CI27
                    ";

            // return $@"
            //         ^BY3,3,99^FT{barcodeFtCoordinates}^BCN,,N,N
            //         ^FH\^FD>:{serialNumber}^FS
            //         ^FT{textFtCoordinates}^A@N,38,36,TT0003M_^FH\^CI28^FD{serialNumber}^FS^CI27
            //         "; Cũ
        }

        public string GenerateSerialBlockAstro(string serialNumber, string barcodeFtCoordinates, string textFtCoordinates)
        {
            if (string.IsNullOrEmpty(serialNumber))
            {
                return ""; // Nếu không có serial, trả về chuỗi rỗng
            }

            // var first = serialNumber[0];
            // var rest = serialNumber.Substring(1);

            // Tạo chuỗi ZPL cho barcode và text serial
            // Lưu ý: Đảm bảo có ký tự xuống dòng (\n) nếu cần để giữ layout đúng



            return $@"
                    ^BY2,3,67^FT{barcodeFtCoordinates}^BCB,,N,N
                    ^FH\^FD>:{serialNumber}^FS
                    ^FT{textFtCoordinates}^A@B,35,36,TT0003M_^FH\^CI28^FD{serialNumber}^FS^CI27";
        }

        public async Task<string> GetTestTemplate(string size, string witdh, string height, string template)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "image/png");

            string testLabel = Uri.EscapeDataString(template);

            var url = $"http://api.labelary.com/v1/printers/{size}dpmm/labels/{witdh}x{height}/0/{testLabel}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images/label");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"label_{Guid.NewGuid()}.png";
                string filePath = Path.Combine(uploadsFolder, fileName);


                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                string res = $"/Images/label/{fileName}";

                return res;
            }
            return "";
        }

        public string CreateQrCode(string content, string ID_Printer)
        {
            Bitmap bitmap;
            if (ID_Printer == "Walter_411")
            {
                bitmap = GenerateQrCodeBitmap(content, 420);
            }
            else if (ID_Printer == "WALTER_INBOX_1")
            {
                // bitmap = GenerateQrCodeBitmap(content, 820);
                bitmap = GenerateQrCodeBitmap(content, 420);
            }
            else
            {
                bitmap = GenerateQrCodeBitmap(content);
            }
            var bitmapMono = ConvertToMonochrome(bitmap);
            var hexBitmap = ExtractMonochromeBitmapToHex(bitmapMono);
            int actualQrCodeWidthPixels = bitmapMono.Width;
            int actualQrCodeHeightPixels = bitmapMono.Height;
            string finalZplCommand = "";
            if (ID_Printer == "Walter_411")
            {
                finalZplCommand = BuildCompleteZplCommand(
                    hexBitmap,
                    actualQrCodeWidthPixels, // Truyền vào đây
                    actualQrCodeHeightPixels, // Truyền vào đây
                    278,
                    1250
                );
            }
            else if (ID_Printer == "WALTER_INBOX_1")
            {
                finalZplCommand = BuildCompleteZplCommand(
                    hexBitmap,
                    actualQrCodeWidthPixels, // Truyền vào đây
                    actualQrCodeHeightPixels, // Truyền vào đây
                    50,
                    100
                );
            }
            else
            {
                finalZplCommand = BuildCompleteZplCommand(
                    hexBitmap,
                    actualQrCodeWidthPixels, // Truyền vào đây
                    actualQrCodeHeightPixels, // Truyền vào đây
                    556,
                    2512
                );
            }
            return finalZplCommand;
        }

        public static Bitmap GenerateQrCodeBitmap(string content, int size = 820)
        {
            Bitmap qrCodeBitmap;
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    qrCodeBitmap = qrCode.GetGraphic(20, Color.Black, Color.White, true);
                }

                Bitmap resizedQrCodeBitmap = new Bitmap(size, size);
                using (Graphics g = Graphics.FromImage(resizedQrCodeBitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(qrCodeBitmap, 0, 0, size, size);
                }
                qrCodeBitmap.Dispose();

                return resizedQrCodeBitmap;
            }
        }

        public static Bitmap ConvertToMonochrome(Bitmap originalBitmap)
        {

            if (originalBitmap == null)
            {
                throw new ArgumentNullException(nameof(originalBitmap), "Bitmap gốc không được null.");
            }
            Bitmap monochromeBitmap = originalBitmap.Clone(
                new Rectangle(0, 0, originalBitmap.Width, originalBitmap.Height),
                PixelFormat.Format1bppIndexed);

            return monochromeBitmap;
        }

        public static string ExtractMonochromeBitmapToHex(Bitmap monochromeBitmap)
        {
            // Đảm bảo bitmap là monochrome (1bpp)
            if (monochromeBitmap.PixelFormat != PixelFormat.Format1bppIndexed)
            {
                throw new ArgumentException("Bitmap phải ở định dạng 1bpp (monochrome).", nameof(monochromeBitmap));
            }

            StringBuilder hexData = new StringBuilder();
            byte currentByte = 0;
            int bitCount = 0;

            // Duyệt qua từng hàng (scanline) của bitmap
            for (int y = 0; y < monochromeBitmap.Height; y++)
            {
                // Duyệt qua từng pixel trong hàng
                for (int x = 0; x < monochromeBitmap.Width; x++)
                {
                    // Lấy màu của pixel tại (x, y)
                    Color pixelColor = monochromeBitmap.GetPixel(x, y);

                    // Xác định bit dựa trên màu sắc.
                    // Nếu pixel là màu đen (giá trị RGB là 0,0,0), đặt bit là 1.
                    // Nếu pixel là màu trắng (giá trị RGB là 255,255,255), bit là 0.
                    // ZPL GFA thường có bit 0 là trắng, bit 1 là đen.
                    if (pixelColor.R == 0 && pixelColor.G == 0 && pixelColor.B == 0) // Màu đen
                    {
                        // Set bit tương ứng trong currentByte.
                        // (7 - bitCount) đảm bảo bit được đặt từ trái sang phải (Most Significant Bit first).
                        currentByte |= (byte)(1 << (7 - bitCount));
                    }
                    // Nếu là màu trắng, bit mặc định là 0, không cần làm gì

                    bitCount++; // Tăng bộ đếm bit

                    // Khi đã thu thập đủ 8 bit (tức là 1 byte), chuyển đổi nó thành chuỗi hex và reset
                    if (bitCount == 8)
                    {
                        hexData.Append(currentByte.ToString("X2")); // "X2" định dạng byte thành 2 ký tự hex (VD: 10 -> 0A, 255 -> FF)
                        currentByte = 0; // Reset byte hiện tại
                        bitCount = 0;    // Reset bộ đếm bit
                    }
                }

                // Sau khi kết thúc một hàng, nếu còn bit lẻ chưa đủ 8 bit,
                // thêm byte cuối cùng vào chuỗi hex (phần còn lại sẽ là 0)
                if (bitCount > 0)
                {
                    hexData.Append(currentByte.ToString("X2"));
                    currentByte = 0;
                    bitCount = 0;
                }
            }

            return hexData.ToString();
        }

        public static string BuildCompleteZplCommand(string hexQrCodeData, int qrCodeWidthPixels, int qrCodeHeightPixels, int xPositionDots, int yPositionDots)
        {
            // 1. Tính toán các thông số cần thiết cho lệnh ^GFA
            // total_bytes: Tổng số byte của dữ liệu hex. Mỗi 2 ký tự hex = 1 byte.
            int totalBytes = hexQrCodeData.Length / 2;

            // bytes_per_row: Số byte trên mỗi dòng của hình ảnh.
            // (Chiều rộng pixel + 7) / 8 để làm tròn lên số nguyên gần nhất của byte.
            int bytesPerRow = (qrCodeWidthPixels + 7) / 8;

            // 2. Bắt đầu xây dựng chuỗi ZPL
            StringBuilder zplCommand = new StringBuilder();

            // ^XA: Bắt đầu định dạng ZPL


            // ^FOx,y: Đặt vị trí gốc của trường (Field Origin)
            // Đây là vị trí góc trên bên trái của hình ảnh QR code trên nhãn.
            zplCommand.AppendFormat("^FO{0},{1}^GFA,", xPositionDots, yPositionDots);

            // ^GFA: Lệnh đồ họa từ file ASCII (Graphic Field from ASCII)
            // Các tham số: total_bytes, total_bytes_transmitted, bytes_per_row, hex_data
            // total_bytes_transmitted thường giống với total_bytes
            zplCommand.AppendFormat("{0},{1},{2},", totalBytes, totalBytes, bytesPerRow);

            // Thêm dữ liệu hex của QR code
            zplCommand.Append(hexQrCodeData);

            // ^FS: Kết thúc trường (Field Separator)
            zplCommand.AppendLine("^FS");

            // ^XZ: Kết thúc định dạng ZPL


            return zplCommand.ToString();
        }
    }
}
