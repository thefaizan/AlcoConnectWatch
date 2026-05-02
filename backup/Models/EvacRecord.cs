using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlcoConnectWatch.Models
{
    [Table("EvacRecords")]
    public class EvacRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(200)]
        public string Workgroup { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Organisation { get; set; }

        [Column(TypeName = "date")]
        public DateTime RosterDate { get; set; }

        [Required]
        [StringLength(100)]
        public string WorkSite { get; set; }

        [StringLength(100)]
        public string WorkStatus { get; set; }

        [StringLength(50)]
        public string Room { get; set; }

        [StringLength(50)]
        public string Mobile { get; set; }

        [StringLength(50)]
        public string ImaOpenInx { get; set; }

        [Required]
        [StringLength(50)]
        public string ExtractedId { get; set; }

        [StringLength(300)]
        public string FileName { get; set; }

        public DateTime ImportedAt { get; set; }
    }
}
