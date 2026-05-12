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
        [HttpGet]
        [Route("stats")]
        public IHttpActionResult GetStats()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var today = DateTime.Today;

                var filesToday = db.FileImportLogs
                    .Count(f => DbFunctions.TruncateTime(f.ImportedAt) == today);

                // Get user's assigned sites
                var userId = Request.Properties.ContainsKey("UserId")
                    ? (int)Request.Properties["UserId"]
                    : 0;

                var sites = new System.Collections.Generic.List<string>();
                if (userId > 0)
                {
                    var user = db.Users.Find(userId);
                    if (user != null && !string.IsNullOrEmpty(user.SiteAccess))
                    {
                        sites = user.SiteAccess
                            .Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                    }
                }

                // Fallback to all sites if no user sites
                if (sites.Count == 0)
                {
                    var siteSetting = db.AppSettings.Find("Sites");
                    sites = siteSetting != null
                        ? siteSetting.SettingValue.Split(',').Select(s => s.Trim()).ToList()
                        : new System.Collections.Generic.List<string> { "Dalgaranga", "Mt Magnet", "Edna May" };
                }

                var latestDate = db.EvacRecords
                    .OrderByDescending(e => e.RosterDate)
                    .Select(e => e.RosterDate)
                    .FirstOrDefault();

                double complianceRate = 0;
                int complianceGaps = 0;

                //if (latestDate != default(DateTime))
                //{
                //    var totalEvac = db.EvacRecords.Count(e => e.RosterDate == latestDate);
                //    if (totalEvac > 0)
                //    {
                //        var evacIds = db.EvacRecords
                //            .Where(e => e.RosterDate == latestDate)
                //            .Select(e => e.ExtractedId)
                //            .ToList();

                //        var alcoIds = db.AlcoConnectRecords
                //            .Where(a => a.TestDate == latestDate)
                //            .Select(a => a.StaffId)
                //            .Distinct()
                //            .ToList();

                //        var alcoIdsTrimmed = alcoIds.Select(id => id.TrimStart('0')).ToHashSet();
                //        var matched = evacIds.Count(id => alcoIdsTrimmed.Contains(id.TrimStart('0')));
                //        complianceRate = Math.Round((double)matched / totalEvac * 100, 1);
                //        complianceGaps = totalEvac - matched;
                //    }
                //}
                if (latestDate != default(DateTime))
                {
                    // Filter Evac by user's sites
                    var evacRecords = db.EvacRecords
                        .Where(e => e.RosterDate == latestDate)
                        .ToList()
                        .Where(e => sites.Any(site =>
                            site.ToLower().Contains("magnet")
                                ? e.WorkSite.ToLower().Contains("magnet")
                                : site.ToLower().Contains("penny")
                                    ? e.WorkSite.ToLower().Contains("penny")
                                    : site.ToLower().Contains("dalgaranga")
                                        ? e.WorkSite.ToLower().Contains("dalgaranga")
                                        : e.WorkSite.ToLower() == site.ToLower()))
                        .ToList();

                    var totalEvac = evacRecords.Count;
                    if (totalEvac > 0)
                    {
                        var evacIds = evacRecords.Select(e => e.ExtractedId).ToList();

                        // Filter AlcoConnect by user's sites
                        var alcoIds = db.AlcoConnectRecords
                            .Where(a => a.TestDate == latestDate)
                            .ToList()
                            .Where(a => sites.Any(site =>
                                site.ToLower().Contains("magnet")
                                    ? a.Site.ToLower().Contains("magnet")
                                    : site.ToLower().Contains("penny")
                                        ? a.Site.ToLower().Contains("penny")
                                        : site.ToLower().Contains("dalgaranga")
                                            ? a.Site.ToLower().Contains("dalgaranga")
                                            : a.Site.ToLower() == site.ToLower()))
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
                    PendingAlerts = complianceGaps,
                      Sites = sites  // ADD THIS
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
