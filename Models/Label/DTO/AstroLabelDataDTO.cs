using System.ComponentModel.DataAnnotations;

namespace SVN_Tools.Models.Label.DTOs
{
    public class AstroLabelDataDto
    {
        public string? Date { get; set; }
        public string? PackageID { get; set; }
        public string? Serial { get; set; }
        public string? ScanDate { get; set; }
        public string? PalletID { get; set; }
        public string? EmployeeID { get; set; }
        public int CountSerial { get; set; }
    }
}