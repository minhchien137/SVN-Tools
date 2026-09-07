namespace SVN_Tools.Models
{
    public class QRRequest
    {
        public string Content { get; set; }
        public int? XPosition { get; set; } // int? để cho phép null, sử dụng default nếu không gửi
        public int? YPosition { get; set; }
        public int? PixelsPerModule { get; set; }
        // Thêm các thuộc tính khác của QRCodeSettings nếu bạn muốn cho phép client tùy chỉnh
        public QRCoder.QRCodeGenerator.ECCLevel? ErrorCorrectionLevel { get; set; } // Sẽ cần using QRCoder;
    }

    public class QRResponse
    {
        public bool Success { get; set; }
        public string ZplCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // Thêm các model khác nếu cần
    public class SimpleQRRequest
    {
        public string Text { get; set; } = string.Empty;
    }
}