using System;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;
using AlcoConnectWatch.Models;
using AlcoConnectWatch.Services;

namespace AlcoConnectWatch.Data
{
    public class DbInitializer : CreateDatabaseIfNotExists<AlcoConnectWatchContext>
    {
        protected override void Seed(AlcoConnectWatchContext context)
        {
            // Create admin user with salted password
            var salt = AuthTokenService.GenerateSalt();
            var hashedPassword = AuthTokenService.HashPassword("admin123", salt);

            context.Users.Add(new User
            {
                Email = "admin@alcoconnectwatch.com",
                PasswordHash = hashedPassword,
                PasswordSalt = salt,
                SiteAccess = "Dalgaranga,Mt Magnet,Edna May",
                Role = "Admin",
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

        // Legacy method - kept for backwards compatibility during migration
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        // New method with salt
        public static string HashPasswordWithSalt(string password, string salt)
        {
            return AuthTokenService.HashPassword(password, salt);
        }
    }
}
