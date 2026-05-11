using System;
using System.Collections.Generic;
using System.Linq;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models.DTOs;

namespace AlcoConnectWatch.Services
{
    public class ComparisonEngine
    {
        public static ReportResponse GenerateReport(DateTime date, string site)
        {
            using (var db = new AlcoConnectWatchContext())
            {

                var isMtMagnet = site.ToLower().Contains("magnet");
                var isPenny = site.ToLower().Contains("penny");
                var isDalgaranga = site.ToLower().Contains("dalgaranga");

                var evacRecords = db.EvacRecords
                    .Where(e => e.RosterDate == date &&
                                (isMtMagnet ? e.WorkSite.ToLower().Contains("magnet") :
                                 isPenny ? e.WorkSite.ToLower().Contains("penny") :
                                 isDalgaranga ? e.WorkSite.ToLower().Contains("dalgaranga") :
                                                   e.WorkSite.ToLower() == site.ToLower()))
                    .ToList();
                var alcoRecords = db.AlcoConnectRecords
    .Where(a => a.TestDate == date &&
                (isMtMagnet ? a.Site.ToLower().Contains("magnet") :
                 isPenny ? a.Site.ToLower().Contains("penny") :
                 isDalgaranga ? a.Site.ToLower().Contains("dalgaranga") :
                                   a.Site.ToLower() == site.ToLower()))
    .ToList();
                //var isMtMagnet = site.ToLower().Contains("magnet");

                //var evacRecords = db.EvacRecords
                //    .Where(e => e.RosterDate == date &&
                //                (isMtMagnet
                //                    ? e.WorkSite.ToLower().Contains("magnet") ? e.WorkSite.ToLower().Contains("dalgaranga") ? e.WorkSite.ToLower().Contains("penny")
                //                    : e.WorkSite.ToLower() == site.ToLower()))
                //    .ToList();

                //var alcoRecords = db.AlcoConnectRecords
                //    .Where(a => a.TestDate == date &&
                //                (isMtMagnet
                //                    ? a.Site.ToLower().Contains("magnet")
                //                    : a.Site.ToLower() == site.ToLower()))
                //    .ToList();

                //var evacRecords = db.EvacRecords
                //    .Where(e => e.RosterDate == date &&
                //                e.WorkSite.ToLower() == site.ToLower())
                //    .ToList();

                //var alcoRecords = db.AlcoConnectRecords
                //    .Where(a => a.TestDate == date &&
                //                a.Site.ToLower().Contains(site.ToLower()))
                //    .ToList();

                var alcoByStaffId = alcoRecords
                    .GroupBy(a => a.StaffId.TrimStart('0'))
                    .ToDictionary(g => g.Key, g => g.ToList());

                var evacOnly = new List<EvacOnlyRecord>();
                var matched = new List<MatchedRecord>();
                var matchedAlcoIds = new HashSet<string>();

                foreach (var evac in evacRecords)
                {
                    var extractedId = evac.ExtractedId.TrimStart('0');
                    if (string.IsNullOrEmpty(extractedId)) extractedId = "0";

                    List<Models.AlcoConnectRecord> alcoMatches;
                    if (alcoByStaffId.TryGetValue(extractedId, out alcoMatches))
                    {
                        matchedAlcoIds.Add(extractedId);
                        var firstTest = alcoMatches.OrderBy(a => a.TestTime).First();
                        matched.Add(new MatchedRecord
                        {
                            ExtractedId = evac.ExtractedId,
                            EvacName = evac.Name,
                            StaffName = firstTest.StaffName,
                            Workgroup = evac.Workgroup,
                            Organisation = evac.Organisation,
                            WorkStatus = evac.WorkStatus,
                            TestTime = firstTest.TestTime.ToString(@"hh\:mm\:ss"),
                            Result = firstTest.Result,
                            Location = firstTest.Location
                        });
                    }
                    else
                    {
                        evacOnly.Add(new EvacOnlyRecord
                        {
                            ExtractedId = evac.ExtractedId,
                            Name = evac.Name,
                            Workgroup = evac.Workgroup,
                            Organisation = evac.Organisation,
                            WorkSite = evac.WorkSite,
                            WorkStatus = evac.WorkStatus,
                            Room = evac.Room,
                            ImaOpenInx = evac.ImaOpenInx,
                            Mobile = evac.Mobile
                        });
                    }
                }

                var alcoOnly = alcoByStaffId
                    .Where(kvp => !matchedAlcoIds.Contains(kvp.Key))
                    .Select(kvp =>
                    {
                        var tests = kvp.Value.OrderBy(a => a.TestTime).ToList();
                        return new AlcoOnlyRecord
                        {
                            StaffId = kvp.Value.First().StaffId,
                            StaffName = kvp.Value.First().StaffName,
                            TestCount = tests.Count,
                            FirstTest = tests.First().TestTime.ToString(@"hh\:mm\:ss"),
                            LastTest = tests.Last().TestTime.ToString(@"hh\:mm\:ss"),
                            Result = tests.First().Result,
                            Location = tests.First().Location
                        };
                    })
                    .ToList();

                return new ReportResponse
                {
                    EvacOnly = evacOnly,
                    AlcoOnly = alcoOnly,
                    Matched = matched
                };
            }
        }
    }
}
