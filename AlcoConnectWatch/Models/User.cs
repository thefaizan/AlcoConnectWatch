using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlcoConnectWatch.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Email { get; set; }

        [Required]
        [StringLength(200)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public string PasswordSalt { get; set; }

        [StringLength(500)]
        public string SiteAccess { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; }
    }
}
