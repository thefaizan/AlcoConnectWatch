using System.Collections.Generic;

namespace AlcoConnectWatch.Models.DTOs
{
    public class ParseResult<T>
    {
        public List<T> Records { get; set; } = new List<T>();
        public bool Success { get; set; }
        public string Error { get; set; }

        // Detailed tracking
        public int TotalRowsInFile { get; set; }
        public int SkippedEmptyRows { get; set; }
        public int SkippedInvalidDate { get; set; }
        public int SkippedOtherErrors { get; set; }

        public string GetDetailedMessage()
        {
            var parts = new List<string>();

            if (TotalRowsInFile > 0)
                parts.Add($"Total rows in file: {TotalRowsInFile}");
            if (SkippedEmptyRows > 0)
                parts.Add($"Empty Name/ID: {SkippedEmptyRows}");
            if (SkippedInvalidDate > 0)
                parts.Add($"Invalid date: {SkippedInvalidDate}");
            if (SkippedOtherErrors > 0)
                parts.Add($"Other errors: {SkippedOtherErrors}");
            if (!string.IsNullOrEmpty(Error))
                parts.Add(Error);

            return parts.Count > 0 ? string.Join(" | ", parts) : null;
        }
    }
}
