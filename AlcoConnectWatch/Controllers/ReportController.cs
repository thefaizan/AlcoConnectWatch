using System;
using System.Web.Http;
using AlcoConnectWatch.Services;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Filters;
using System.Linq;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/report")]
    [RequireAuth]
    public class ReportController : ApiController
    {
        [HttpPost]
        [Route("generate")]
        public IHttpActionResult Generate(ReportRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Date) || string.IsNullOrEmpty(request.Site))
                return BadRequest("Date and site are required");

            DateTime date;
            if (!DateTime.TryParse(request.Date, out date))
                return BadRequest("Invalid date format");

            var report = ComparisonEngine.GenerateReport(date, request.Site);
            return Ok(report);
        }

        [HttpGet]
        [Route("sites")]
        public IHttpActionResult GetSites()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                // Get current user from auth token
                var userId = Request.Properties.ContainsKey("UserId")
                    ? (int)Request.Properties["UserId"]
                    : 0;

                if (userId > 0)
                {
                    var user = db.Users.Find(userId);
                    if (user != null && !string.IsNullOrEmpty(user.SiteAccess))
                    {
                        // Return only user's assigned sites
                        var userSites = user.SiteAccess
                            .Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        return Ok(userSites);
                    }
                }
                var siteSetting = db.AppSettings.Find("Sites");
                if (siteSetting != null)
                {
                    var sites = siteSetting.SettingValue
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    return Ok(sites);
                }
                return Ok(new[] { "Dalgaranga", "Mt Magnet", "Edna May" });
            }
        }

        [HttpGet]
        [Route("available-dates")]
        public IHttpActionResult GetAvailableDates()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var dates = db.EvacRecords
                    .Select(e => e.RosterDate)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .Take(30)
                    .ToList()
                    .Select(d => d.ToString("yyyy-MM-dd"))
                    .ToList();
                return Ok(dates);
            }
        }
    }
}
