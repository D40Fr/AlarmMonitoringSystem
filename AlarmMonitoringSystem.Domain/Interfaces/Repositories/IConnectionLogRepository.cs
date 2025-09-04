using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Domain.Interfaces.Repositories
{
    public interface IConnectionLogRepository : IBaseRepository<ConnectionLog>
    {
        Task<IEnumerable<ConnectionLog>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default);
        Task<IEnumerable<ConnectionLog>> GetClientConnectionHistoryAsync(
            Guid clientId,
            int count,
            CancellationToken cancellationToken = default);
        Task<ConnectionLog?> GetLastConnectionLogAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task CleanupOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
    }
}
