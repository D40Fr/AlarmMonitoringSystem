using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Domain.Interfaces.Repositories
{
    public interface IAlarmRepository : IBaseRepository<Alarm>
    {
        Task<Alarm?> GetByAlarmIdAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetBySeverityAsync(AlarmSeverity severity, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetByTypeAsync(AlarmType type, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetUnacknowledgedAlarmsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetAlarmsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> GetRecentAlarmsAsync(int count, CancellationToken cancellationToken = default);
        Task<bool> AlarmExistsAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default);
        Task AcknowledgeAlarmAsync(Guid alarmId, string acknowledgedBy, CancellationToken cancellationToken = default);
        Task<int> GetAlarmCountBySeverityAsync(AlarmSeverity severity, CancellationToken cancellationToken = default);
        Task<IEnumerable<Alarm>> SearchAlarmsAsync(string searchTerm, CancellationToken cancellationToken = default);
    }
}
