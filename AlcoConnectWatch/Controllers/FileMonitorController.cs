using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Services;
using AlcoConnectWatch.Filters;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/filemonitor")]
    [RequireAuth]
    public class FileMonitorController : ApiController
    {
        // Get current user's assigned sites
        private System.Collections.Generic.List<string> GetUserSites(AlcoConnectWatchContext db)
        {
            var userId = Request.Properties.ContainsKey("UserId")
                ? (int)Request.Properties["UserId"]
                : 0;

            if (userId > 0)
            {
                var user = db.Users.Find(userId);
                if (user != null && !string.IsNullOrEmpty(user.SiteAccess))
                {
                    return user.SiteAccess
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }

            var siteSetting = db.AppSettings.Find("Sites");
            return siteSetting != null
                ? siteSetting.SettingValue.Split(',').Select(s => s.Trim()).ToList()
                : new System.Collections.Generic.List<string> { "Dalgaranga", "Mt Magnet", "Edna May" };
        }

        // Check if filename belongs to user's sites
        private bool FileMatchesSites(string fileName, System.Collections.Generic.List<string> userSites)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            foreach (var site in userSites)
            {
                if (site.Equals("Dalgaranga", StringComparison.OrdinalIgnoreCase) &&
                    (fileName.IndexOf("DAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     fileName.IndexOf("Dalgaranga", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
                if (site.Equals("Mt Magnet", StringComparison.OrdinalIgnoreCase) &&
                    fileName.IndexOf("MTM", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (site.Equals("Edna May", StringComparison.OrdinalIgnoreCase) &&
                    fileName.IndexOf("EDM", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        [HttpGet]
        [Route("status")]
        public IHttpActionResult GetStatus()
        {
            var watcher = FileWatcherService.Instance;
            var today = DateTime.Today;

            using (var db = new AlcoConnectWatchContext())
            {
                var userSites = GetUserSites(db);

                var todayLogs = db.FileImportLogs
                    .Where(f => DbFunctions.TruncateTime(f.ImportedAt) == today)
                    .ToList()
                    .Where(f => FileMatchesSites(f.FileName, userSites))
                    .ToList();

                var status = new FileMonitorStatus
                {
                    Status = watcher.IsRunning ? "running" : "stopped",
                    WatchFolder = watcher.WatchFolder ?? "Not configured",
                    ScanInterval = watcher.ScanIntervalMinutes,
                    LastScan = watcher.LastScan.HasValue
                        ? FormatTimeAgo(watcher.LastScan.Value)
                        : "Never",
                    FilesDetectedToday = todayLogs.Count,
                    ErrorsToday = todayLogs.Count(f => f.Status == "Error"),
                    EvacFilesToday = todayLogs.Count(f => f.FileType == "Evac" && f.Status == "Success"),
                    AlcoFilesToday = todayLogs.Count(f => f.FileType == "AlcoConnect" && f.Status == "Success")
                };

                return Ok(status);
            }
        }

        [HttpGet]
        [Route("recent-files")]
        public IHttpActionResult GetRecentFiles()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var userSites = GetUserSites(db);

                var files = db.FileImportLogs
                    .OrderByDescending(f => f.ImportedAt)
                    .Take(50) // Get more, then filter
                    .ToList()
                    .Where(f => FileMatchesSites(f.FileName, userSites))
                    .Take(20)
                    .Select(f => new RecentFileDTO
                    {
                        Name = f.FileName,
                        Type = f.FileType,
                        Site = ExtractSiteFromFileName(f.FileName),
                        Rows = f.RowCount,
                        Status = f.Status == "Success" ? "success" : "error",
                        Time = FormatTimeAgo(f.ImportedAt)
                    })
                    .ToList();

                return Ok(files);
            }
        }

        [HttpPost]
        [Route("restart")]
        public IHttpActionResult Restart()
        {
            FileWatcherService.Instance.Restart();
            return Ok(new { success = true, message = "File watcher restarted" });
        }

        private string ExtractSiteFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Unknown";
            if (fileName.IndexOf("DAL", StringComparison.OrdinalIgnoreCase) >= 0) return "Dalgaranga";
            if (fileName.IndexOf("MTM", StringComparison.OrdinalIgnoreCase) >= 0) return "Mt Magnet";
            if (fileName.IndexOf("EDM", StringComparison.OrdinalIgnoreCase) >= 0) return "Edna May";
            if (fileName.IndexOf("Dalgaranga", StringComparison.OrdinalIgnoreCase) >= 0) return "Dalgaranga";
            return "Unknown";
        }

        private string FormatTimeAgo(DateTime dateTime)
        {
            var diff = DateTime.Now - dateTime;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
            return $"{(int)diff.TotalDays} days ago";
        }
    }
}
