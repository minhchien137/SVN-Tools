namespace SVN_Tools.Models
{
    public class PrintRequest
    {
        public List<PrintTemViewModel> ViewModels { get; set; }
        public int Copies { get; set; }
        public string PrinterID { get; set; }
    }
}
