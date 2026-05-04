using System;
using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Filters;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/importlogs")]
    [RequireAuth]
    public class ImportLogController : ApiController
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
        [Route("")]
        public IHttpActionResult GetAll()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var userSites = GetUserSites(db);

                var logs = db.FileImportLogs
                    .OrderByDescending(f => f.ImportedAt)
                    .Take(200) // Get more, then filter
                    .ToList()
                    .Where(f => FileMatchesSites(f.FileName, userSites))
                    .Take(100)
                    .Select(f => new
                    {
                        f.Id,
                        FileName = f.FileName,
                        FileType = f.FileType,
                        RowCount = f.RowCount,
                        DuplicatesSkipped = f.DuplicatesSkipped,
                        Status = f.Status,
                        ErrorMessage = f.ErrorMessage,
                        ImportedAt = f.ImportedAt
                    })
                    .ToList();

                return Ok(logs);
            }
        }
    }
}
