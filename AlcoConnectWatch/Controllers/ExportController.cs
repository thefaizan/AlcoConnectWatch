using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Services;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/export")]
    public class ExportController : ApiController
    {
        [HttpPost]
        [Route("excel")]
        public HttpResponseMessage ExportExcel(ReportRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Date) || string.IsNullOrEmpty(request.Site))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Date and site are required");

            DateTime date;
            if (!DateTime.TryParse(request.Date, out date))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid date format");

            var report = ComparisonEngine.GenerateReport(date, request.Site);
            var bytes = ExportService.ExportToExcel(report, request.Site, request.Date);

            var fileName = $"Compliance_Report_{request.Site.Replace(" ", "_")}_{date:yyyy-MM-dd}.xlsx";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(bytes);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            return response;
        }

        [HttpPost]
        [Route("csv")]
        public HttpResponseMessage ExportCsv(ExportCsvRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Date) || string.IsNullOrEmpty(request.Site))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Date and site are required");

            DateTime date;
            if (!DateTime.TryParse(request.Date, out date))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid date format");

            var report = ComparisonEngine.GenerateReport(date, request.Site);
            var bytes = ExportService.ExportToCsv(report, request.Group ?? "evac-only");

            var fileName = $"Compliance_Report_{request.Site.Replace(" ", "_")}_{date:yyyy-MM-dd}_{request.Group ?? "all"}.csv";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new ByteArrayContent(bytes);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = fileName };
            return response;
        }
    }

    public class ExportCsvRequest
    {
        public string Date { get; set; }
        public string Site { get; set; }
        public string Group { get; set; }
    }
}
