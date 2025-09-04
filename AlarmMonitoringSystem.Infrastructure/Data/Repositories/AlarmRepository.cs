// AlarmMonitoringSystem.Infrastructure/Data/Repositories/AlarmRepository.cs
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace AlarmMonitoringSystem.Infrastructure.Data.Repositories
{
    public class AlarmRepository : BaseRepository<Alarm>, IAlarmRepository
    {
        public AlarmRepository(AlarmMonitoringDbContext context) : base(context)
        {
        }

        public async Task<Alarm?> GetByAlarmIdAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(alarmId))
                return null;

            return await _dbSet
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.AlarmId == alarmId.Trim() && a.ClientId == clientId, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetBySeverityAsync(AlarmSeverity severity, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.Severity == severity && a.IsActive)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetByTypeAsync(AlarmType type, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.Type == type && a.IsActive)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetUnacknowledgedAlarmsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.IsActive && !a.IsAcknowledged)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetAlarmsByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.AlarmTime >= startDate && a.AlarmTime <= endDate)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetRecentAlarmsAsync(int count, CancellationToken cancellationToken = default)
        {
            if (count <= 0) count = 10;

            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.AlarmTime)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> AlarmExistsAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(alarmId))
                return false;

            return await _dbSet
                .AnyAsync(a => a.AlarmId == alarmId.Trim() && a.ClientId == clientId, cancellationToken);
        }

        public async Task AcknowledgeAlarmAsync(Guid alarmId, string acknowledgedBy, CancellationToken cancellationToken = default)
        {
            var alarm = await _dbSet.FindAsync(new object[] { alarmId }, cancellationToken);
            if (alarm != null && !alarm.IsAcknowledged)
            {
                alarm.IsAcknowledged = true;
                alarm.AcknowledgedAt = DateTime.UtcNow;
                alarm.UpdatedAt = DateTime.UtcNow;
            }
        }

        public async Task<int> GetAlarmCountBySeverityAsync(AlarmSeverity severity, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(a => a.Severity == severity && a.IsActive, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> SearchAlarmsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveAlarmsAsync(cancellationToken);

            var term = searchTerm.Trim().ToLower();

            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.IsActive && (
                    a.Title.ToLower().Contains(term) ||
                    a.Message.ToLower().Contains(term) ||
                    a.AlarmId.ToLower().Contains(term) ||
                    a.Client.Name.ToLower().Contains(term) ||
                    a.Client.ClientId.ToLower().Contains(term)
                ))
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        // Override GetByIdAsync to include client data
        public override async Task<Alarm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        // Get alarms with priority (Critical first, then High, etc.)
        public async Task<IEnumerable<Alarm>> GetAlarmsByPriorityAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.IsActive && !a.IsAcknowledged)
                .OrderBy(a => a.Severity == AlarmSeverity.Critical ? 0 :
                             a.Severity == AlarmSeverity.High ? 1 :
                             a.Severity == AlarmSeverity.Medium ? 2 : 3)
                .ThenByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        // Get alarm statistics
        public async Task<Dictionary<AlarmSeverity, int>> GetAlarmCountsBySeverityAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.IsActive)
                .GroupBy(a => a.Severity)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
        }

        public async Task<Dictionary<AlarmType, int>> GetAlarmCountsByTypeAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.IsActive)
                .GroupBy(a => a.Type)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
        }

        // Get alarms for a client within date range
        public async Task<IEnumerable<Alarm>> GetClientAlarmsByDateRangeAsync(
            Guid clientId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.ClientId == clientId &&
                           a.AlarmTime >= startDate &&
                           a.AlarmTime <= endDate)
                .OrderByDescending(a => a.AlarmTime)
                .ToListAsync(cancellationToken);
        }

        // Get top alarms by severity
        public async Task<IEnumerable<Alarm>> GetTopAlarmsBySeverityAsync(
            AlarmSeverity minSeverity,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0) count = 10;

            return await _dbSet
                .Include(a => a.Client)
                .Where(a => a.IsActive && (int)a.Severity >= (int)minSeverity)
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.AlarmTime)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        // Bulk acknowledge alarms
        public async Task BulkAcknowledgeAlarmsAsync(
            IEnumerable<Guid> alarmIds,
            string acknowledgedBy,
            CancellationToken cancellationToken = default)
        {
            var ids = alarmIds.ToList();
            if (!ids.Any()) return;

            var alarms = await _dbSet
                .Where(a => ids.Contains(a.Id) && !a.IsAcknowledged)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var alarm in alarms)
            {
                alarm.IsAcknowledged = true;
                alarm.AcknowledgedAt = now;
                alarm.UpdatedAt = now;
            }
        }

        // Get alarm trends (alarms per day for the last N days)
        public async Task<Dictionary<DateTime, int>> GetAlarmTrendsAsync(
            int days,
            CancellationToken cancellationToken = default)
        {
            if (days <= 0) days = 7;

            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var endDate = DateTime.UtcNow.Date.AddDays(1);

            var alarms = await _dbSet
                .Where(a => a.AlarmTime >= startDate && a.AlarmTime < endDate)
                .Select(a => a.AlarmTime.Date)
                .ToListAsync(cancellationToken);

            return alarms
                .GroupBy(date => date)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}