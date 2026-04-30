using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Services;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/settings")]
    public class SettingsController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var settings = db.AppSettings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

                string val;
                var dto = new SettingsDTO
                {
                    WatchFolder = settings.TryGetValue("WatchFolderPath", out val) ? val : @"C:\AlcoConnectWatch\WatchFolder",
                    ScanInterval = settings.TryGetValue("ScanIntervalMinutes", out val) && int.TryParse(val, out int interval) ? interval : 5,
                    DbServer = "localhost\\SQLEXPRESS",
                    DbName = "AlcoConnectWatchDB",
                    Sites = settings.TryGetValue("Sites", out val)
                        ? val.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList()
                        : new System.Collections.Generic.List<string> { "Dalgaranga", "Mt Magnet", "Edna May" }
                };

                return Ok(dto);
            }
        }

        [HttpPut]
        [Route("")]
        public IHttpActionResult Save(SettingsDTO request)
        {
            if (request == null) return BadRequest("Settings are required");

            using (var db = new AlcoConnectWatchContext())
            {
                UpsertSetting(db, "WatchFolderPath", request.WatchFolder);
                UpsertSetting(db, "ScanIntervalMinutes", request.ScanInterval.ToString());

                if (request.Sites != null && request.Sites.Any())
                    UpsertSetting(db, "Sites", string.Join(",", request.Sites));

                db.SaveChanges();
            }

            FileWatcherService.Instance.Restart();

            return Ok(new { success = true });
        }

        private void UpsertSetting(AlcoConnectWatchContext db, string key, string value)
        {
            var setting = db.AppSettings.Find(key);
            if (setting != null)
            {
                setting.SettingValue = value;
            }
            else
            {
                db.AppSettings.Add(new AppSetting { SettingKey = key, SettingValue = value });
            }
        }
    }
}
