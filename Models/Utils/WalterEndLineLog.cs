using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVN_Tools.Models.Utils
{
    [Table("SVN_WALTER_END_LINE_LOG")]
    public class SVN_WALTER_END_LINE_LOG
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        // map đúng tên cột + báo cho EF biết cột này được sinh từ DB (DEFAULT SYSDATETIME())
        [Column("date_time")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime DateTime { get; set; }

        [Column("content")]
        public string Content { get; set; }
    }

    public class LogRequest
    {
        // Chỉ cần trường content để client gửi lên
        public string Content { get; set; }
    }
}
