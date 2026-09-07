using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVN_Tools.Models.Utils
{
    [Table("SVN_project_pass")]
    public class ProjectPasswordModel
    {
        [Key]
        public int ID { get; set; }

        public string Password_name { get; set; }
        public string Password_value { get; set; }

    }
}
