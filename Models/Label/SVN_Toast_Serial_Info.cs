using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVN_Tools.Models.Label
{
    [Table("SVN_Toast_Scan_Rule")]
    public class SVNToastScanRule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Column("prefix")]
        public string Prefix { get; set; } = "";

        [Column("min_seq")]
        public int MinSeq { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("note")]
        [StringLength(200)]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    [Table("SVN_Toast_Serial_Info")]
    public class SVNToastSerialInfo
    {
        [Key]
        [Column("serial_number")]
        [Required]
        [StringLength(100)]
        public string SerialNumber { get; set; }

        [Column("work_order")]
        [StringLength(100)]
        public string? WorkOrder { get; set; }

        [Column("FCT_status")]
        [StringLength(100)]
        public string? FCTStatus { get; set; }

        [Column("FCT_status_datetime")]
        public DateTime? FCTStatusDatetime { get; set; }

        [Column("FQC_status")]
        [StringLength(100)]
        public string? FQCStatus { get; set; }

        [Column("FQC_status_datetime")]
        public DateTime? FQCStatusDatetime { get; set; }

        [Column("update_by_svncode")]
        public string? updateBySVNCode { get; set; }
    }

    public class FctSubmitReq
    {
        public string Serial { get; set; }
        public string Status { get; set; } // "OK" | "NG"
    }

    public class FqcUpdateRequest
    {
        public string serialNumber { get; set; }
        public string status { get; set; }
    }

    public class UpdateSerialStatusRequest
    {
        public string Serial { get; set; } = "";
        public string? SVNCode { get; set; }
        public string? FCT { get; set; }
        public string? FQC { get; set; }
    }
}