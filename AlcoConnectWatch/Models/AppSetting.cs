using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlcoConnectWatch.Models
{
    [Table("AppSettings")]
    public class AppSetting
    {
        [Key]
        [StringLength(100)]
        public string SettingKey { get; set; }

        [StringLength(1000)]
        public string SettingValue { get; set; }
    }
}
