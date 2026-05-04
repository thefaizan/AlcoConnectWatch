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
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var logs = db.FileImportLogs
                    .OrderByDescending(f => f.ImportedAt)
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
