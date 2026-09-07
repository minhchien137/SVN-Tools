using System.ComponentModel.DataAnnotations;

namespace SVN_Tools.Models.Label.DTOs
{
    public class WHLabelInfoDTO
    {
        public string Label_id { get; set; }
        public string Item_code { get; set; }
        public string Date { get; set; }
        public string Qty { get; set; }
        public string location { get; set; }

    }
}