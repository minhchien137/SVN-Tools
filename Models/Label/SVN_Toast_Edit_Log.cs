using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVN_Tools.Models.Label
{
    [Table("SVN_Toast_Edit_Log")]
    public class SVNToastEditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Column("ActionType")]
        public string ActionType { get; set; } = "";

        [Required]
        [StringLength(100)]
        [Column("SerialCode")]
        public string SerialCode { get; set; } = "";

        [StringLength(100)]
        [Column("RelatedSerial")]
        public string? RelatedSerial { get; set; }

        [StringLength(20)]
        [Column("Station")]
        public string? Station { get; set; }

        [Column("OldValue")]
        public string? OldValue { get; set; }

        [Column("NewValue")]
        public string? NewValue { get; set; }

        [StringLength(500)]
        [Column("Reason")]
        public string? Reason { get; set; }

        [StringLength(100)]
        [Column("EditedBy")]
        public string? EditedBy { get; set; }

        [Column("EditedAt")]
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;
    }

    public static class ToastEditActionType
    {
        public const string ReplaceComponent = "ReplaceComponent";
        public const string RenameSerial = "RenameSerial";
        public const string AssignSerial = "AssignSerial";
    }

    public class ReplaceComponentRequest
    {
        public string Serial { get; set; } = "";
        public string Station { get; set; } = ""; // "WIP" | "FG"
        public string OldLotNumber { get; set; } = "";
        public string NewProductCode { get; set; } = "";
        public string NewLotNumber { get; set; } = "";
        public string Reason { get; set; } = "";
        public string SVNCode { get; set; } = "";
    }

    public class RenameSerialRequest
    {
        public string OldSerial { get; set; } = "";
        public string NewSerial { get; set; } = "";
        public string Station { get; set; } = ""; // "WIP" | "FG"
        public string Reason { get; set; } = "";
        public string SVNCode { get; set; } = "";
    }

    public class AssignSerialToWipRequest
    {
        public int WipId { get; set; }
        public string Serial { get; set; } = "";
        public string Reason { get; set; } = "";
        public string SVNCode { get; set; } = "";
    }
}
