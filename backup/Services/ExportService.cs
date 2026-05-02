using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using AlcoConnectWatch.Models.DTOs;

namespace AlcoConnectWatch.Services
{
    public class ExportService
    {
        public static byte[] ExportToExcel(ReportResponse report, string site, string date)
        {
            using (var workbook = new XLWorkbook())
            {
                var evacSheet = workbook.Worksheets.Add("Evac Only");
                BuildEvacOnlySheet(evacSheet, report.EvacOnly);

                var alcoSheet = workbook.Worksheets.Add("AlcoConnect Only");
                BuildAlcoOnlySheet(alcoSheet, report.AlcoOnly);

                var matchedSheet = workbook.Worksheets.Add("Matched");
                BuildMatchedSheet(matchedSheet, report.Matched);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public static byte[] ExportToCsv(ReportResponse report, string group)
        {
            var sb = new StringBuilder();

            switch (group)
            {
                case "evac-only":
                    sb.AppendLine("Staff ID,Name,Workgroup,Organisation,Work Site,Work Status,Room,IMAOpenINX,Mobile");
                    foreach (var r in report.EvacOnly)
                        sb.AppendLine($"\"{r.ExtractedId}\",\"{r.Name}\",\"{r.Workgroup}\",\"{r.Organisation}\",\"{r.WorkSite}\",\"{r.WorkStatus}\",\"{r.Room}\",\"{r.ImaOpenInx}\",\"{r.Mobile}\"");
                    break;

                case "alco-only":
                    sb.AppendLine("Staff ID,Staff Name,Test Count,First Test,Last Test,Result,Location");
                    foreach (var r in report.AlcoOnly)
                        sb.AppendLine($"\"{r.StaffId}\",\"{r.StaffName}\",{r.TestCount},\"{r.FirstTest}\",\"{r.LastTest}\",\"{r.Result}\",\"{r.Location}\"");
                    break;

                case "matched":
                    sb.AppendLine("Staff ID,Evac Name,AlcoConnect Name,Workgroup,Organisation,Work Status,Test Time,Result,Location");
                    foreach (var r in report.Matched)
                        sb.AppendLine($"\"{r.ExtractedId}\",\"{r.EvacName}\",\"{r.StaffName}\",\"{r.Workgroup}\",\"{r.Organisation}\",\"{r.WorkStatus}\",\"{r.TestTime}\",\"{r.Result}\",\"{r.Location}\"");
                    break;

                default:
                    sb.AppendLine("Staff ID,Name,Workgroup,Organisation,Work Site,Work Status,Room,IMAOpenINX,Mobile");
                    foreach (var r in report.EvacOnly)
                        sb.AppendLine($"\"{r.ExtractedId}\",\"{r.Name}\",\"{r.Workgroup}\",\"{r.Organisation}\",\"{r.WorkSite}\",\"{r.WorkStatus}\",\"{r.Room}\",\"{r.ImaOpenInx}\",\"{r.Mobile}\"");
                    break;
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static void BuildEvacOnlySheet(IXLWorksheet sheet, List<EvacOnlyRecord> records)
        {
            var headers = new[] { "Staff ID", "Name", "Workgroup", "Organisation", "Work Site", "Work Status", "Room", "IMAOpenINX", "Mobile" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            }

            for (int r = 0; r < records.Count; r++)
            {
                var rec = records[r];
                sheet.Cell(r + 2, 1).Value = rec.ExtractedId;
                sheet.Cell(r + 2, 2).Value = rec.Name;
                sheet.Cell(r + 2, 3).Value = rec.Workgroup;
                sheet.Cell(r + 2, 4).Value = rec.Organisation;
                sheet.Cell(r + 2, 5).Value = rec.WorkSite;
                sheet.Cell(r + 2, 6).Value = rec.WorkStatus;
                sheet.Cell(r + 2, 7).Value = rec.Room;
                sheet.Cell(r + 2, 8).Value = rec.ImaOpenInx;
                sheet.Cell(r + 2, 9).Value = rec.Mobile;
            }

            sheet.Columns().AdjustToContents();
        }

        private static void BuildAlcoOnlySheet(IXLWorksheet sheet, List<AlcoOnlyRecord> records)
        {
            var headers = new[] { "Staff ID", "Staff Name", "Test Count", "First Test", "Last Test", "Result", "Location" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
            }

            for (int r = 0; r < records.Count; r++)
            {
                var rec = records[r];
                sheet.Cell(r + 2, 1).Value = rec.StaffId;
                sheet.Cell(r + 2, 2).Value = rec.StaffName;
                sheet.Cell(r + 2, 3).Value = rec.TestCount;
                sheet.Cell(r + 2, 4).Value = rec.FirstTest;
                sheet.Cell(r + 2, 5).Value = rec.LastTest;
                sheet.Cell(r + 2, 6).Value = rec.Result;
                sheet.Cell(r + 2, 7).Value = rec.Location;
            }

            sheet.Columns().AdjustToContents();
        }

        private static void BuildMatchedSheet(IXLWorksheet sheet, List<MatchedRecord> records)
        {
            var headers = new[] { "Staff ID", "Evac Name", "AlcoConnect Name", "Workgroup", "Organisation", "Work Status", "Test Time", "Result", "Location" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = headers[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
                sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
            }

            for (int r = 0; r < records.Count; r++)
            {
                var rec = records[r];
                sheet.Cell(r + 2, 1).Value = rec.ExtractedId;
                sheet.Cell(r + 2, 2).Value = rec.EvacName;
                sheet.Cell(r + 2, 3).Value = rec.StaffName;
                sheet.Cell(r + 2, 4).Value = rec.Workgroup;
                sheet.Cell(r + 2, 5).Value = rec.Organisation;
                sheet.Cell(r + 2, 6).Value = rec.WorkStatus;
                sheet.Cell(r + 2, 7).Value = rec.TestTime;
                sheet.Cell(r + 2, 8).Value = rec.Result;
                sheet.Cell(r + 2, 9).Value = rec.Location;
            }

            sheet.Columns().AdjustToContents();
        }
    }
}
