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
        [HttpGet]
        [Route("status")]
        public IHttpActionResult GetStatus()
        {
            var watcher = FileWatcherService.Instance;
            var today = DateTime.Today;

            using (var db = new AlcoConnectWatchContext())
            {
                var todayLogs = db.FileImportLogs
                    .Where(f => DbFunctions.TruncateTime(f.ImportedAt) == today)
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
                var files = db.FileImportLogs
                    .OrderByDescending(f => f.ImportedAt)
                    .Take(20)
                    .ToList()
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
            if (fileName.IndexOf("breathalyser", StringComparison.OrdinalIgnoreCase) >= 0) return "breathalyser";
            if (fileName.IndexOf("Evac", StringComparison.OrdinalIgnoreCase) >= 0) return "Evac";
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
