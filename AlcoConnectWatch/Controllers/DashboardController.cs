using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Filters;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/dashboard")]
    [RequireAuth]
    public class DashboardController : ApiController
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

            // Fallback to all sites
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
        [Route("stats")]
        public IHttpActionResult GetStats()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var today = DateTime.Today;
                var userSites = GetUserSites(db);

                // Filter files by user's sites
                var todayLogs = db.FileImportLogs
                    .Where(f => DbFunctions.TruncateTime(f.ImportedAt) == today)
                    .ToList();
                var filesToday = todayLogs.Count(f => FileMatchesSites(f.FileName, userSites));

                // Get latest date for user's sites
                var latestDate = db.EvacRecords
                    .Where(e => userSites.Contains(e.WorkSite))
                    .OrderByDescending(e => e.RosterDate)
                    .Select(e => e.RosterDate)
                    .FirstOrDefault();

                double complianceRate = 0;
                int complianceGaps = 0;

                if (latestDate != default(DateTime))
                {
                    var totalEvac = db.EvacRecords.Count(e => e.RosterDate == latestDate && userSites.Contains(e.WorkSite));
                    if (totalEvac > 0)
                    {
                        var evacIds = db.EvacRecords
                            .Where(e => e.RosterDate == latestDate && userSites.Contains(e.WorkSite))
                            .Select(e => e.ExtractedId)
                            .ToList();

                        var alcoIds = db.AlcoConnectRecords
                            .Where(a => a.TestDate == latestDate && userSites.Contains(a.Site))
                            .Select(a => a.StaffId)
                            .Distinct()
                            .ToList();

                        var alcoIdsTrimmed = alcoIds.Select(id => id.TrimStart('0')).ToHashSet();
                        var matched = evacIds.Count(id => alcoIdsTrimmed.Contains(id.TrimStart('0')));
                        complianceRate = Math.Round((double)matched / totalEvac * 100, 1);
                        complianceGaps = totalEvac - matched;
                    }
                }

                return Ok(new DashboardStats
                {
                    FilesProcessed = filesToday,
                    ActiveSites = userSites.Count,
                    ComplianceRate = complianceRate,
                    PendingAlerts = complianceGaps
                });
            }
        }

        [HttpGet]
        [Route("activity")]
        public IHttpActionResult GetRecentActivity()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var userSites = GetUserSites(db);

                // Get logs and filter by user's sites
                var logs = db.FileImportLogs
                    .OrderByDescending(f => f.ImportedAt)
                    .Take(50) // Get more, then filter
                    .ToList()
                    .Where(f => FileMatchesSites(f.FileName, userSites))
                    .Take(10)
                    .ToList();

                var activity = logs.Select(log =>
                {
                    string icon, color, title;
                    if (log.Status == "Error")
                    {
                        icon = "bi-exclamation-triangle";
                        color = "red";
                        title = "Import error";
                    }
                    else if (log.FileType == "Evac")
                    {
                        icon = "bi-file-earmark-check";
                        color = "green";
                        title = "Evac Report imported";
                    }
                    else
                    {
                        icon = "bi-shield-check";
                        color = "gold";
                        title = "Breathalyser file imported";
                    }

                    var desc = log.Status == "Error"
                        ? log.ErrorMessage
                        : $"{log.RowCount} records" +
                          (log.DuplicatesSkipped > 0 ? $" ({log.DuplicatesSkipped} dupes skipped)" : "");

                    return new ActivityItem
                    {
                        Icon = icon,
                        Color = color,
                        Title = title,
                        Desc = desc,
                        Time = FormatTimeAgo(log.ImportedAt)
                    };
                }).ToList();

                return Ok(activity);
            }
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
