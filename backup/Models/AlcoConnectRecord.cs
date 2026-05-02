using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlcoConnectWatch.Models
{
    [Table("AlcoConnectRecords")]
    public class AlcoConnectRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Site { get; set; }

        [Required]
        [StringLength(50)]
        public string StaffId { get; set; }

        [StringLength(200)]
        public string StaffName { get; set; }

        [StringLength(200)]
        public string JobTitle { get; set; }

        [StringLength(50)]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Email { get; set; }

        [StringLength(200)]
        public string Manager { get; set; }

        [StringLength(50)]
        public string MachineType { get; set; }

        [Column(TypeName = "date")]
        public DateTime TestDate { get; set; }

        public TimeSpan TestTime { get; set; }

        [StringLength(50)]
        public string Result { get; set; }

        [StringLength(50)]
        public string SerialNumber { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [StringLength(300)]
        public string FileName { get; set; }

        public DateTime ImportedAt { get; set; }
    }
}
