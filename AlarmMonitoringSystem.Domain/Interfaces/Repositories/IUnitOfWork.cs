using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IClientRepository Clients { get; }
        IAlarmRepository Alarms { get; }
        IConnectionLogRepository ConnectionLogs { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
