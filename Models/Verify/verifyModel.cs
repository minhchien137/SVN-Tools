using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SVN_Tools.Models.Verify
{
    [Table("SVN_Verify_Employee_Data")]
    public class VerifyEmployeeData
    {
        [Key]
        public int Id { get; set; }
        public string SVNCODE { get; set; }
        public string Area { get; set; }
        public string Date { get; set; }
    }
}
