using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SVN_Tools.Models.Verify
{
    [Table("SVN_Iot_verify_employee_data")]
    public class IotVerifyEmployeeData
    {
        [Key]
        public int Id { get; set; }
        public string emp_code { get; set; }
        public string emp_name { get; set; }
        public string Dept { get; set; }
    }
}
