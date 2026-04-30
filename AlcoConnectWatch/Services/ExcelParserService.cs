using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using AlcoConnectWatch.Models;

namespace AlcoConnectWatch.Services
{
    public class ExcelParserService
    {
        private static readonly string[] EvacRequiredColumns =
            { "workgroup", "Name", "Organisation", "RosterDate", "Work Site", "WorkStatus", "Room", "mobile", "IMAOpenINX" };

        private static readonly string[] AlcoRequiredColumns =
            { "Site", "Staff ID", "Staff Name", "Date", "Time", "Result" };

        public static string DetectFileType(string fileName)
        {
            if (fileName.IndexOf("Evac Report", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Evac";
            if (fileName.IndexOf("breathalyser", StringComparison.OrdinalIgnoreCase) >= 0)
                return "AlcoConnect";
            return null;
        }

        public static string ExtractNumericId(string imaOpenInx)
        {
            if (string.IsNullOrWhiteSpace(imaOpenInx))
                return "";
            var digits = Regex.Replace(imaOpenInx, @"[^\d]", "");
            return digits.TrimStart('0').Length > 0 ? digits.TrimStart('0') : "0";
        }

        public static List<EvacRecord> ParseEvacFile(string filePath, out string error)
        {
            error = null;
            var records = new List<EvacRecord>();
            var fileName = Path.GetFileName(filePath);

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheets.First();
                    var headerRow = worksheet.Row(1);
                    var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int col = 1; col <= worksheet.LastColumnUsed().ColumnNumber(); col++)
                    {
                        var headerText = headerRow.Cell(col).GetString().Trim();
                        if (!string.IsNullOrEmpty(headerText))
                            headers[headerText] = col;
                    }

                    foreach (var requiredCol in EvacRequiredColumns)
                    {
                        if (!headers.ContainsKey(requiredCol))
                        {
                            error = $"Missing required column: {requiredCol}";
                            return null;
                        }
                    }

                    var lastRow = worksheet.LastRowUsed().RowNumber();
                    var now = DateTime.Now;

                    for (int row = 2; row <= lastRow; row++)
                    {
                        var name = worksheet.Cell(row, headers["Name"]).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        var imaOpenInx = worksheet.Cell(row, headers["IMAOpenINX"]).GetString().Trim();
                        var rosterDateStr = worksheet.Cell(row, headers["RosterDate"]).GetString().Trim();

                        DateTime rosterDate;
                        if (!TryParseDate(rosterDateStr, out rosterDate))
                        {
                            try { rosterDate = worksheet.Cell(row, headers["RosterDate"]).GetDateTime(); }
                            catch { continue; }
                        }

                        records.Add(new EvacRecord
                        {
                            Workgroup = worksheet.Cell(row, headers["workgroup"]).GetString().Trim(),
                            Name = name,
                            Organisation = worksheet.Cell(row, headers["Organisation"]).GetString().Trim(),
                            RosterDate = rosterDate,
                            WorkSite = worksheet.Cell(row, headers["Work Site"]).GetString().Trim(),
                            WorkStatus = worksheet.Cell(row, headers["WorkStatus"]).GetString().Trim(),
                            Room = worksheet.Cell(row, headers["Room"]).GetString().Trim(),
                            Mobile = worksheet.Cell(row, headers["mobile"]).GetString().Trim(),
                            ImaOpenInx = imaOpenInx,
                            ExtractedId = ExtractNumericId(imaOpenInx),
                            FileName = fileName,
                            ImportedAt = now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }

            return records;
        }

        public static List<AlcoConnectRecord> ParseAlcoConnectFile(string filePath, out string error)
        {
            error = null;
            var records = new List<AlcoConnectRecord>();
            var fileName = Path.GetFileName(filePath);

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheets.First();
                    var headerRow = worksheet.Row(1);
                    var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int col = 1; col <= worksheet.LastColumnUsed().ColumnNumber(); col++)
                    {
                        var headerText = headerRow.Cell(col).GetString().Trim();
                        if (!string.IsNullOrEmpty(headerText))
                            headers[headerText] = col;
                    }

                    foreach (var requiredCol in AlcoRequiredColumns)
                    {
                        if (!headers.ContainsKey(requiredCol))
                        {
                            error = $"Missing required column: {requiredCol}";
                            return null;
                        }
                    }

                    var lastRow = worksheet.LastRowUsed().RowNumber();
                    var now = DateTime.Now;

                    for (int row = 2; row <= lastRow; row++)
                    {
                        var staffId = worksheet.Cell(row, headers["Staff ID"]).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(staffId)) continue;

                        var dateStr = worksheet.Cell(row, headers["Date"]).GetString().Trim();
                        DateTime testDate;
                        if (!TryParseDate(dateStr, out testDate))
                        {
                            try { testDate = worksheet.Cell(row, headers["Date"]).GetDateTime(); }
                            catch { continue; }
                        }

                        var timeStr = worksheet.Cell(row, headers["Time"]).GetString().Trim();
                        TimeSpan testTime;
                        if (!TimeSpan.TryParse(timeStr, out testTime))
                            testTime = TimeSpan.Zero;

                        records.Add(new AlcoConnectRecord
                        {
                            Site = worksheet.Cell(row, headers["Site"]).GetString().Trim(),
                            StaffId = staffId,
                            StaffName = worksheet.Cell(row, headers["Staff Name"]).GetString().Trim(),
                            JobTitle = headers.ContainsKey("Job Title") ? worksheet.Cell(row, headers["Job Title"]).GetString().Trim() : "",
                            Phone = headers.ContainsKey("Phone") ? worksheet.Cell(row, headers["Phone"]).GetString().Trim() : "",
                            Email = headers.ContainsKey("Email") ? worksheet.Cell(row, headers["Email"]).GetString().Trim() : "",
                            Manager = headers.ContainsKey("Manager") ? worksheet.Cell(row, headers["Manager"]).GetString().Trim() : "",
                            MachineType = headers.ContainsKey("Machine Type") ? worksheet.Cell(row, headers["Machine Type"]).GetString().Trim() : "",
                            TestDate = testDate,
                            TestTime = testTime,
                            Result = worksheet.Cell(row, headers["Result"]).GetString().Trim(),
                            SerialNumber = headers.ContainsKey("Serial Number") ? worksheet.Cell(row, headers["Serial Number"]).GetString().Trim() : "",
                            Location = headers.ContainsKey("Location") ? worksheet.Cell(row, headers["Location"]).GetString().Trim() : "",
                            FileName = fileName,
                            ImportedAt = now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }

            return records;
        }

        private static bool TryParseDate(string dateStr, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateStr)) return false;

            string[] formats = {
                "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy",
                "dd-MM-yyyy", "d-MM-yyyy", "dd-M-yyyy",
                "yyyy-MM-dd",
                "ddMMMyyyy", "dd MMM yyyy", "d MMM yyyy",
                "MM/dd/yyyy"
            };

            return DateTime.TryParseExact(dateStr, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out result);
        }
    }
}
