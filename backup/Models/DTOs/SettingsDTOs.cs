using System.Collections.Generic;

namespace AlcoConnectWatch.Models.DTOs
{
    public class SettingsDTO
    {
        public string WatchFolder { get; set; }
        public int ScanInterval { get; set; }
        public string DbServer { get; set; }
        public string DbName { get; set; }
        public List<string> Sites { get; set; }
    }

    public class FileMonitorStatus
    {
        public string Status { get; set; }
        public string WatchFolder { get; set; }
        public int ScanInterval { get; set; }
        public string LastScan { get; set; }
        public int FilesDetectedToday { get; set; }
        public int ErrorsToday { get; set; }
        public int EvacFilesToday { get; set; }
        public int AlcoFilesToday { get; set; }
    }

    public class RecentFileDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Site { get; set; }
        public int Rows { get; set; }
        public string Status { get; set; }
        public string Time { get; set; }
    }
}
