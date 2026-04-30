namespace AlcoConnectWatch.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using AlcoConnectWatch.Data;
    using AlcoConnectWatch.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<AlcoConnectWatchContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(AlcoConnectWatchContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    Email = "admin@alcoconnectwatch.com",
                    PasswordHash = DbInitializer.HashPassword("admin123"),
                    SiteAccess = "Dalgaranga,Mt Magnet,Edna May",
                    CreatedAt = DateTime.Now
                });
            }

            if (!context.AppSettings.Any())
            {
                context.AppSettings.Add(new AppSetting { SettingKey = "WatchFolderPath", SettingValue = @"C:\AlcoConnectWatch\WatchFolder" });
                context.AppSettings.Add(new AppSetting { SettingKey = "ScanIntervalMinutes", SettingValue = "5" });
                context.AppSettings.Add(new AppSetting { SettingKey = "Sites", SettingValue = "Dalgaranga,Mt Magnet,Edna May" });
            }

            context.SaveChanges();
        }
    }
}
