using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlcoConnectWatch.Models
{
    [Table("FileImportLogs")]
    public class FileImportLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string FileName { get; set; }

        [Required]
        [StringLength(20)]
        public string FileType { get; set; }

        public int RowCount { get; set; }

        public int DuplicatesSkipped { get; set; }

        // Detailed tracking fields
        public int TotalRowsInFile { get; set; }

        public int SkippedEmptyRows { get; set; }

        public int SkippedInvalidDate { get; set; }

        public int SkippedOtherErrors { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [StringLength(1000)]
        public string ErrorMessage { get; set; }

        public DateTime ImportedAt { get; set; }
    }
}
