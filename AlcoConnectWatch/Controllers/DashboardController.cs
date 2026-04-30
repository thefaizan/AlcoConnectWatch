using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models.DTOs;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        [HttpGet]
        [Route("stats")]
        public IHttpActionResult GetStats()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var today = DateTime.Today;

                var filesToday = db.FileImportLogs
                    .Count(f => DbFunctions.TruncateTime(f.ImportedAt) == today);

                var siteSetting = db.AppSettings.Find("Sites");
                var sites = siteSetting != null
                    ? siteSetting.SettingValue.Split(',').Select(s => s.Trim()).ToList()
                    : new System.Collections.Generic.List<string>();

                var latestDate = db.EvacRecords
                    .OrderByDescending(e => e.RosterDate)
                    .Select(e => e.RosterDate)
                    .FirstOrDefault();

                double complianceRate = 0;
                int complianceGaps = 0;

                if (latestDate != default(DateTime))
                {
                    var totalEvac = db.EvacRecords.Count(e => e.RosterDate == latestDate);
                    if (totalEvac > 0)
                    {
                        var evacIds = db.EvacRecords
                            .Where(e => e.RosterDate == latestDate)
                            .Select(e => e.ExtractedId)
                            .ToList();

                        var alcoIds = db.AlcoConnectRecords
                            .Where(a => a.TestDate == latestDate)
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
                    ActiveSites = sites.Count,
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
                var logs = db.FileImportLogs
                    .OrderByDescending(f => f.ImportedAt)
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
