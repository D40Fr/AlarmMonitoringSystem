using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.ValueObjects;

namespace AlarmMonitoringSystem.Domain.Interfaces.Services
{
    public interface IAlarmService
    {
        // Alarm processing
        Task<Alarm> ProcessAlarmAsync(Guid clientId, AlarmData alarmData, CancellationToken cancellationToken = default);
        Task<Alarm> ProcessAlarmAsync(string clientId, AlarmData alarmData, CancellationToken cancellationToken = default);
        Task<bool> IsAlarmDuplicateAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default);

        // Alarm retrieval
        Task<Alarm?> GetAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default);
        Task<Alarm?> GetAlarmByAlarmIdAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetClientAlarmsAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetAlarmsBySeverityAsync(AlarmSeverity severity, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetAlarmsByTypeAsync(AlarmType type, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetUnacknowledgedAlarmsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetRecentAlarmsAsync(int count, CancellationToken cancellationToken = default);

        // Alarm management
        Task<Alarm> AcknowledgeAlarmAsync(Guid alarmId, string acknowledgedBy, CancellationToken cancellationToken = default);
        Task<Alarm> DeactivateAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetAlarmsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> SearchAlarmsAsync(string searchTerm, CancellationToken cancellationToken = default);

        // Statistics
        Task<int> GetAlarmCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetActiveAlarmCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetUnacknowledgedAlarmCountAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<AlarmSeverity, int>> GetAlarmCountsBySeverityAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<AlarmType, int>> GetAlarmCountsByTypeAsync(CancellationToken cancellationToken = default);
    }
}
