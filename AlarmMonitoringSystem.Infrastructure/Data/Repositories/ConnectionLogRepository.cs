// AlarmMonitoringSystem.Infrastructure/Data/Repositories/ConnectionLogRepository.cs
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace AlarmMonitoringSystem.Infrastructure.Data.Repositories
{
    public class ConnectionLogRepository : BaseRepository<ConnectionLog>, IConnectionLogRepository
    {
        public ConnectionLogRepository(AlarmMonitoringDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ConnectionLog>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.ClientId == clientId)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.Status == status)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetByLogLevelAsync(LogLevel logLevel, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.LogLevel == logLevel)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.LogTime >= startDate && cl.LogTime <= endDate)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default)
        {
            if (count <= 0) count = 50;

            return await _dbSet
                .Include(cl => cl.Client)
                .OrderByDescending(cl => cl.LogTime)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetClientConnectionHistoryAsync(
            Guid clientId,
            int count,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0) count = 20;

            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.ClientId == clientId)
                .OrderByDescending(cl => cl.LogTime)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<ConnectionLog?> GetLastConnectionLogAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.ClientId == clientId)
                .OrderByDescending(cl => cl.LogTime)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task CleanupOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
        {
            var oldLogs = await _dbSet
                .Where(cl => cl.LogTime < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Any())
            {
                _dbSet.RemoveRange(oldLogs);
            }
        }

        // Override GetByIdAsync to include client data
        public override async Task<ConnectionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .FirstOrDefaultAsync(cl => cl.Id == id, cancellationToken);
        }

        // Get logs by multiple log levels
        public async Task<IEnumerable<ConnectionLog>> GetByLogLevelsAsync(
            IEnumerable<LogLevel> logLevels,
            CancellationToken cancellationToken = default)
        {
            var levels = logLevels.ToList();
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => levels.Contains(cl.LogLevel))
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        // Get error logs for troubleshooting
        public async Task<IEnumerable<ConnectionLog>> GetErrorLogsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.LogLevel == LogLevel.Error || cl.LogLevel == LogLevel.Critical)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        // Get connection events (Connected/Disconnected only)
        public async Task<IEnumerable<ConnectionLog>> GetConnectionEventsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.Status == ConnectionStatus.Connected || cl.Status == ConnectionStatus.Disconnected)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        // Get connection statistics
        public async Task<Dictionary<ConnectionStatus, int>> GetConnectionStatusCountsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .GroupBy(cl => cl.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
        }

        public async Task<Dictionary<LogLevel, int>> GetLogLevelCountsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .GroupBy(cl => cl.LogLevel)
                .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
        }

        // Get logs for specific client and date range
        public async Task<IEnumerable<ConnectionLog>> GetClientLogsByDateRangeAsync(
            Guid clientId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.ClientId == clientId &&
                            cl.LogTime >= startDate &&
                            cl.LogTime <= endDate)
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        // Search logs by message content
        public async Task<IEnumerable<ConnectionLog>> SearchLogsAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetRecentLogsAsync(50, cancellationToken);

            var term = searchTerm.Trim().ToLower();

            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => (cl.Message != null && cl.Message.ToLower().Contains(term)) ||
                            (cl.Details != null && cl.Details.ToLower().Contains(term)) ||
                            cl.Client.Name.ToLower().Contains(term) ||
                            cl.Client.ClientId.ToLower().Contains(term))
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        // Get client uptime statistics
        public async Task<TimeSpan?> GetClientUptimeAsync(
            Guid clientId,
            DateTime fromDate,
            CancellationToken cancellationToken = default)
        {
            var connectionEvents = await _dbSet
                .Where(cl => cl.ClientId == clientId &&
                            cl.LogTime >= fromDate &&
                            (cl.Status == ConnectionStatus.Connected || cl.Status == ConnectionStatus.Disconnected))
                .OrderBy(cl => cl.LogTime)
                .Select(cl => new { cl.LogTime, cl.Status })
                .ToListAsync(cancellationToken);

            if (!connectionEvents.Any())
                return null;

            TimeSpan totalUptime = TimeSpan.Zero;
            DateTime? connectionStart = null;
            var now = DateTime.UtcNow;

            foreach (var evt in connectionEvents)
            {
                if (evt.Status == ConnectionStatus.Connected)
                {
                    connectionStart = evt.LogTime;
                }
                else if (evt.Status == ConnectionStatus.Disconnected && connectionStart.HasValue)
                {
                    totalUptime += evt.LogTime - connectionStart.Value;
                    connectionStart = null;
                }
            }

            // If still connected, add time until now
            if (connectionStart.HasValue)
            {
                totalUptime += now - connectionStart.Value;
            }

            return totalUptime;
        }

        // Get logs by IP address
        public async Task<IEnumerable<ConnectionLog>> GetLogsByIpAddressAsync(
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return Enumerable.Empty<ConnectionLog>();

            return await _dbSet
                .Include(cl => cl.Client)
                .Where(cl => cl.IpAddress == ipAddress.Trim())
                .OrderByDescending(cl => cl.LogTime)
                .ToListAsync(cancellationToken);
        }

        // Get connection frequency for a client
        public async Task<int> GetClientConnectionFrequencyAsync(
            Guid clientId,
            TimeSpan period,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow - period;

            return await _dbSet
                .CountAsync(cl => cl.ClientId == clientId &&
                                 cl.Status == ConnectionStatus.Connected &&
                                 cl.LogTime >= startTime,
                           cancellationToken);
        }

        // Cleanup logs older than specified days, but keep at least one log per client
        public async Task CleanupOldLogsWithRetentionAsync(
            int retentionDays,
            CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            // Get clients and their latest log
            var latestLogsPerClient = await _dbSet
                .GroupBy(cl => cl.ClientId)
                .Select(g => g.OrderByDescending(cl => cl.LogTime).First().Id)
                .ToListAsync(cancellationToken);

            // Delete old logs except the latest one for each client
            var logsToDelete = await _dbSet
                .Where(cl => cl.LogTime < cutoffDate && !latestLogsPerClient.Contains(cl.Id))
                .ToListAsync(cancellationToken);

            if (logsToDelete.Any())
            {
                _dbSet.RemoveRange(logsToDelete);
            }
        }
    }
}