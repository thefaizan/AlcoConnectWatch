using System;
using System.Collections.Generic;
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

        [HttpGet]
        [Route("debug")]
        public IHttpActionResult Debug()
        {
            var watcher = FileWatcherService.Instance;
            var watchFolder = watcher.WatchFolder;
            var filesList = new List<string>();
            var allFiles = new List<string>();
            string errorMsg = "";

            try
            {
                if (!string.IsNullOrEmpty(watchFolder) && System.IO.Directory.Exists(watchFolder))
                {
                    // Get ALL files
                    var all = System.IO.Directory.GetFiles(watchFolder);
                    foreach (var f in all)
                    {
                        allFiles.Add(System.IO.Path.GetFileName(f));
                    }

                    // Get xlsx files
                    var xlsx = System.IO.Directory.GetFiles(watchFolder, "*.xlsx");
                    foreach (var f in xlsx)
                    {
                        filesList.Add(System.IO.Path.GetFileName(f));
                    }
                }
            }
            catch (System.Exception ex)
            {
                errorMsg = ex.Message;
            }

            return Ok(new
            {
                watchFolder = watchFolder,
                folderExists = !string.IsNullOrEmpty(watchFolder) && System.IO.Directory.Exists(watchFolder),
                xlsxFiles = filesList,
                allFiles = allFiles,
                error = errorMsg
            });
        }

        [HttpPost]
        [Route("scan-now")]
        public IHttpActionResult ScanNow()
        {
            var watcher = FileWatcherService.Instance;
            var watchFolder = watcher.WatchFolder;
            var results = new List<object>();

            try
            {
                if (string.IsNullOrEmpty(watchFolder) || !System.IO.Directory.Exists(watchFolder))
                {
                    return Ok(new { success = false, error = "Watch folder not found: " + watchFolder });
                }

                var files = System.IO.Directory.GetFiles(watchFolder, "*.xlsx");

                if (files.Length == 0)
                {
                    return Ok(new { success = false, error = "No xlsx files found in folder", folder = watchFolder });
                }

                foreach (var filePath in files)
                {
                    var fileName = System.IO.Path.GetFileName(filePath);
                    var fileType = ExcelParserService.DetectFileType(fileName);

                    results.Add(new
                    {
                        fileName = fileName,
                        detectedType = fileType ?? "Unknown",
                        willProcess = fileType != null
                    });
                }

                return Ok(new { success = true, files = results });
            }
            catch (System.Exception ex)
            {
                return Ok(new { success = false, error = ex.Message });
            }
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
