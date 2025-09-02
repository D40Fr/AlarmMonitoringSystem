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
    public interface IConnectionLogService
    {
        // Logging operations
        Task LogConnectionEventAsync(ConnectionEvent connectionEvent, CancellationToken cancellationToken = default);
        Task LogClientConnectedAsync(Guid clientId, string? ipAddress = null, int? port = null, CancellationToken cancellationToken = default);
        Task LogClientDisconnectedAsync(Guid clientId, string? reason = null, CancellationToken cancellationToken = default);
        Task LogConnectionErrorAsync(Guid clientId, string errorMessage, string? details = null, CancellationToken cancellationToken = default);

        // Log retrieval
        Task<IEnumerable<ConnectionLog>> GetConnectionLogsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetClientConnectionLogsAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetLogsByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetLogsByLevelAsync(LogLevel logLevel, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetClientConnectionHistoryAsync(Guid clientId, int count, CancellationToken cancellationToken = default);

        // Log management
        Task<ConnectionLog?> GetLastConnectionLogAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task CleanupOldLogsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
        Task<int> GetLogCountAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<LogLevel, int>> GetLogCountsByLevelAsync(CancellationToken cancellationToken = default);
    }
}
