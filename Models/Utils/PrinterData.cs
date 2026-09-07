namespace SVN_Tools.Models.Utils
{

    public class PrinterData
    {
        public string ID_Printer { get; set; }
        public string Name_Printer { get; set; }
        public string IP_Printer { get; set; }
        public string MAC_Printer { get; set; }
        public string Port_Printer { get; set; }
        public string Size { get; set; } //3x3 4x6 4x8
        public string Type { get; set; }
        public string ZPL_Temp { get; set; }
        public string DPL_Temp { get; set; }
        public string template { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string quantity { get; set; }
        public string target { get; set; }
    }
}
