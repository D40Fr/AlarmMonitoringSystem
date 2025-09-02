using AlarmMonitoringSystem.Application.DTOs;

namespace AlarmMonitoringSystem.Web.Models
{
    public class DashboardViewModel
    {
        public List<ClientDto> Clients { get; set; } = new();
        public List<AlarmDto> RecentAlarms { get; set; } = new();
        public List<ConnectionLogDto> RecentConnectionLogs { get; set; } = new();
        public DashboardStatistics Statistics { get; set; } = new();
    }

    public class DashboardStatistics
    {
        public int TotalClients { get; set; }
        public int ConnectedClients { get; set; }
        public int DisconnectedClients => TotalClients - ConnectedClients;
        public int TotalAlarms { get; set; }
        public int ActiveAlarms { get; set; }
        public int UnacknowledgedAlarms { get; set; }
        public int TcpServerPort { get; set; }
        public TimeSpan TcpServerUptime { get; set; }
        public long TotalMessagesReceived { get; set; }

        public string UptimeDisplay
        {
            get
            {
                if (TcpServerUptime.TotalDays >= 1)
                    return $"{(int)TcpServerUptime.TotalDays}d {TcpServerUptime.Hours}h {TcpServerUptime.Minutes}m";
                else if (TcpServerUptime.TotalHours >= 1)
                    return $"{(int)TcpServerUptime.TotalHours}h {TcpServerUptime.Minutes}m";
                else
                    return $"{(int)TcpServerUptime.TotalMinutes}m";
            }
        }

        public double ClientConnectionPercentage => TotalClients > 0 ? (double)ConnectedClients / TotalClients * 100 : 0;
        public double AlarmAcknowledgedPercentage => TotalAlarms > 0 ? (double)(TotalAlarms - UnacknowledgedAlarms) / TotalAlarms * 100 : 0;
    }
}