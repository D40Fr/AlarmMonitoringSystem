using AlarmMonitoringSystem.Application.DTOs;

namespace AlarmMonitoringSystem.Web.Models
{
    public class ClientDetailsViewModel
    {
        public ClientDto Client { get; set; } = new();
        public List<AlarmDto> Alarms { get; set; } = new();
        public List<ConnectionLogDto> ConnectionLogs { get; set; } = new();

        public int TotalAlarms => Alarms.Count;
        public int ActiveAlarms => Alarms.Count(a => a.IsActive);
        public int UnacknowledgedAlarms => Alarms.Count(a => a.IsActive && !a.IsAcknowledged);
        public int CriticalAlarms => Alarms.Count(a => a.IsActive && a.Severity == Domain.Enums.AlarmSeverity.Critical);
        public DateTime? LastAlarmTime => Alarms.Where(a => a.IsActive).Max(a => a.AlarmTime as DateTime?);

        public List<AlarmDto> RecentCriticalAlarms => Alarms
            .Where(a => a.IsActive && a.Severity == Domain.Enums.AlarmSeverity.Critical)
            .OrderByDescending(a => a.AlarmTime)
            .Take(5)
            .ToList();
    }
}