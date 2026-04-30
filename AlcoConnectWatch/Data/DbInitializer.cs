using System;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using AlcoConnectWatch.Models;

namespace AlcoConnectWatch.Data
{
    public class DbInitializer : CreateDatabaseIfNotExists<AlcoConnectWatchContext>
    {
        protected override void Seed(AlcoConnectWatchContext context)
        {
            context.Users.Add(new User
            {
                Email = "admin@alcoconnectwatch.com",
                PasswordHash = HashPassword("admin123"),
                SiteAccess = "Dalgaranga,Mt Magnet,Edna May",
                CreatedAt = DateTime.Now
            });

            context.AppSettings.Add(new AppSetting
            {
                SettingKey = "WatchFolderPath",
                SettingValue = @"C:\AlcoConnectWatch\WatchFolder"
            });

            context.AppSettings.Add(new AppSetting
            {
                SettingKey = "ScanIntervalMinutes",
                SettingValue = "5"
            });

            context.AppSettings.Add(new AppSetting
            {
                SettingKey = "Sites",
                SettingValue = "Dalgaranga,Mt Magnet,Edna May"
            });

            context.SaveChanges();
            base.Seed(context);
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
