namespace SVN_Tools.Models
{
    public class PrintToastLabelRequest
    {
        public string PartNumber { get; set; }
        public string ModelNumber { get; set; }
        public string ToastPONumber { get; set; }
        public string Quantity { get; set; }
        public string LotID { get; set; }
        public string PartDesc { get; set; }
        public bool Print150Seri { get; set; }
        public string AllSeri1 { get; set; }
        public string AllSeri2 { get; set; }
        public string AllSeri3 { get; set; }
        public int Copies { get; set; }
        public string PrinterID { get; set; }
        public string PalletID { get; set; }
    }
}
