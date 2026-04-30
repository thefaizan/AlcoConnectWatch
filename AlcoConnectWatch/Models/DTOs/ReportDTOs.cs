using System.Collections.Generic;

namespace AlcoConnectWatch.Models.DTOs
{
    public class ReportRequest
    {
        public string Date { get; set; }
        public string Site { get; set; }
    }

    public class ReportResponse
    {
        public List<EvacOnlyRecord> EvacOnly { get; set; }
        public List<AlcoOnlyRecord> AlcoOnly { get; set; }
        public List<MatchedRecord> Matched { get; set; }
    }

    public class EvacOnlyRecord
    {
        public string ExtractedId { get; set; }
        public string Name { get; set; }
        public string Workgroup { get; set; }
        public string Organisation { get; set; }
        public string WorkSite { get; set; }
        public string WorkStatus { get; set; }
        public string Room { get; set; }
        public string ImaOpenInx { get; set; }
        public string Mobile { get; set; }
    }

    public class AlcoOnlyRecord
    {
        public string StaffId { get; set; }
        public string StaffName { get; set; }
        public int TestCount { get; set; }
        public string FirstTest { get; set; }
        public string LastTest { get; set; }
        public string Result { get; set; }
        public string Location { get; set; }
    }

    public class MatchedRecord
    {
        public string ExtractedId { get; set; }
        public string EvacName { get; set; }
        public string StaffName { get; set; }
        public string Workgroup { get; set; }
        public string Organisation { get; set; }
        public string WorkStatus { get; set; }
        public string TestTime { get; set; }
        public string Result { get; set; }
        public string Location { get; set; }
    }
}
