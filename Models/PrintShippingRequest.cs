namespace SVN_Tools.Models
{
    public class PrintShippingRequest
    {
        public List<PrintShippingViewModel> ViewModels { get; set; }
        public int Copies { get; set; }
        public string PrinterID { get; set; }
        public string DateCode { get; set; }
    }
}
