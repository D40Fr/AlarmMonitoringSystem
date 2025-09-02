using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Domain.Interfaces.Services
{
    public interface INotificationService
    {
        // Real-time notifications
        Task NotifyClientConnectedAsync(ClientInfo clientInfo, CancellationToken cancellationToken = default);
        Task NotifyClientDisconnectedAsync(string clientId, string? reason = null, CancellationToken cancellationToken = default);
        Task NotifyClientStatusChangedAsync(string clientId, ConnectionStatus oldStatus, ConnectionStatus newStatus, CancellationToken cancellationToken = default);
        Task NotifyNewAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default);
        Task NotifyAlarmAcknowledgedAsync(Guid alarmId, string acknowledgedBy, CancellationToken cancellationToken = default);

        // Group notifications
        Task NotifyGroupAsync(string groupName, string method, object data, CancellationToken cancellationToken = default);
        Task NotifyAllAsync(string method, object data, CancellationToken cancellationToken = default);

        // Connection management
        Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
        Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default);
        Task<int> GetActiveConnectionCountAsync(CancellationToken cancellationToken = default);
    }
}
