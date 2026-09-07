using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SVN_Tools.Models.Label
{
    [Table("WHLabelInfo")]
    public class WHLabelInfo
    {
        [Key]
        public string Label_id { get; set; }
        public string Item_code { get; set; }
        public string Date { get; set; }
        public string Qty { get; set; }

        public string location { get; set; }
    }
}
