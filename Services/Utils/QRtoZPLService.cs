using System;
using System.Drawing;
using System.IO;
using QRCoder;

namespace SVN_Tools.Services.Utils
{
    public class QRtoZPLService
    {
        // Method gốc (giữ nguyên để backward compatibility)
        public string GenerateZplFromText(string content)
        {
            return GenerateZplFromText(content, 50, 50, 10);
        }

        // Method với tham số tùy chỉnh vị trí và kích thước
        public string GenerateZplFromText(string content, int xPosition, int yPosition, int pixelsPerModule)
        {
            // Tạo QR code generator
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            // Tạo QR code bitmap với kích thước tùy chỉnh
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            // Chuyển đổi byte array thành Bitmap
            using var ms = new MemoryStream(qrCodeImage);
            using var bitmap = new Bitmap(ms);

            // Chuyển đổi bitmap sang monochrome
            var monochromeBytes = ConvertBitmapToMonochrome(bitmap, out int widthBytes, out int totalBytes);

            // Tạo ZPL string với vị trí tùy chỉnh
            string zpl = $"\n^FO{xPosition},{yPosition}\n^GFA,{totalBytes},{totalBytes},{widthBytes},";
            zpl += BitConverter.ToString(monochromeBytes).Replace("-", "") + "\n";
            // string zpl = $"^XA\n^FO{xPosition},{yPosition}\n^GFA,{totalBytes},{totalBytes},{widthBytes},";
            // zpl += BitConverter.ToString(monochromeBytes).Replace("-", "") + "\n^XZ";

            return zpl;
        }

        // Method với QRCodeSettings object để dễ dàng mở rộng
        public string GenerateZplFromText(string content, QRCodeSettings settings)
        {
            // Tạo QR code generator
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, settings.ErrorCorrectionLevel);

            // Tạo QR code bitmap
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(settings.PixelsPerModule);

            // Chuyển đổi byte array thành Bitmap
            using var ms = new MemoryStream(qrCodeImage);
            using var bitmap = new Bitmap(ms);

            // Chuyển đổi bitmap sang monochrome
            var monochromeBytes = ConvertBitmapToMonochrome(bitmap, out int widthBytes, out int totalBytes);

            // Tạo ZPL string với settings tùy chỉnh
            string zpl = $"^XA\n^FO{settings.XPosition},{settings.YPosition}\n^GFA,{totalBytes},{totalBytes},{widthBytes},";
            zpl += BitConverter.ToString(monochromeBytes).Replace("-", "");

            // Thêm field separator nếu có
            if (!string.IsNullOrEmpty(settings.FieldSeparator))
            {
                zpl += $"\n{settings.FieldSeparator}";
            }

            zpl += "\n^XZ";

            return zpl;
        }

        // Method chỉ tạo phần GFA (không có ^XA và ^XZ) để nhúng vào template có sẵn
        public string GenerateGFAOnly(string content, int xPosition, int yPosition, int pixelsPerModule)
        {
            // Tạo QR code generator
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            // Tạo QR code bitmap
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            // Chuyển đổi byte array thành Bitmap
            using var ms = new MemoryStream(qrCodeImage);
            using var bitmap = new Bitmap(ms);

            // Chuyển đổi bitmap sang monochrome
            var monochromeBytes = ConvertBitmapToMonochrome(bitmap, out int widthBytes, out int totalBytes);

            // Tạo chỉ phần GFA
            string gfa = $"^FO{xPosition},{yPosition}\n^GFA,{totalBytes},{totalBytes},{widthBytes},";
            gfa += BitConverter.ToString(monochromeBytes).Replace("-", "");

            return gfa;
        }

        // Method tính toán kích thước QR code
        public QRCodeDimensions CalculateQRDimensions(string content, int pixelsPerModule)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            using var ms = new MemoryStream(qrCodeImage);
            using var bitmap = new Bitmap(ms);

            return new QRCodeDimensions
            {
                Width = bitmap.Width,
                Height = bitmap.Height,
                WidthInDots = bitmap.Width,
                HeightInDots = bitmap.Height
            };
        }

        static byte[] ConvertBitmapToMonochrome(Bitmap bitmap, out int bytesPerRow, out int totalBytes)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            bytesPerRow = (width + 7) / 8;
            totalBytes = bytesPerRow * height;
            byte[] imageData = new byte[totalBytes];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    int byteIndex = y * bytesPerRow + x / 8;
                    int bitIndex = 7 - (x % 8);

                    // Nếu pixel tối (đen) thì set bit
                    if (pixel.GetBrightness() < 0.5f)
                    {
                        imageData[byteIndex] |= (byte)(1 << bitIndex);
                    }
                }
            }

            return imageData;
        }
    }

    // Settings class cho QR code
    public class QRCodeSettings
    {
        public int XPosition { get; set; } = 50;
        public int YPosition { get; set; } = 50;
        public int PixelsPerModule { get; set; } = 10;
        public QRCodeGenerator.ECCLevel ErrorCorrectionLevel { get; set; } = QRCodeGenerator.ECCLevel.Q;
        public string FieldSeparator { get; set; } = string.Empty;

        // Constructor mặc định
        public QRCodeSettings() { }

        // Constructor với các tham số cơ bản
        public QRCodeSettings(int x, int y, int size)
        {
            XPosition = x;
            YPosition = y;
            PixelsPerModule = size;
        }

        // Constructor đầy đủ
        public QRCodeSettings(int x, int y, int size, QRCodeGenerator.ECCLevel errorLevel, string separator = "")
        {
            XPosition = x;
            YPosition = y;
            PixelsPerModule = size;
            ErrorCorrectionLevel = errorLevel;
            FieldSeparator = separator;
        }
    }

    // Class cho thông tin kích thước
    public class QRCodeDimensions
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int WidthInDots { get; set; }
        public int HeightInDots { get; set; }
    }
}