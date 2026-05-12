using System.Collections.Generic;

namespace AlcoConnectWatch.Models.DTOs
{
    public class DashboardStats
    {
        public int FilesProcessed { get; set; }
        public int ActiveSites { get; set; }
        public double ComplianceRate { get; set; }
        public int PendingAlerts { get; set; }

        public List<string> Sites { get; set; }
    }

    public class ActivityItem
    {
        public string Icon { get; set; }
        public string Color { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public string Time { get; set; }
    }

    public class DashboardResponse
    {
        public DashboardStats Stats { get; set; }
        public List<ActivityItem> RecentActivity { get; set; }
    }
}
