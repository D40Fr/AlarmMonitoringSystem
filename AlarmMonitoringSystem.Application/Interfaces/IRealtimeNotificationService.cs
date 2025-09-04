// AlarmMonitoringSystem.Application/Interfaces/IRealtimeNotificationService.cs
using AlarmMonitoringSystem.Application.DTOs;

namespace AlarmMonitoringSystem.Application.Interfaces
{
    public interface IRealtimeNotificationService
    {
        // Alarm notifications
        Task NotifyNewAlarmAsync(AlarmDto alarm);
        Task NotifyAlarmAcknowledgedAsync(Guid alarmId, string acknowledgedBy);

        // Client status notifications
        Task NotifyClientConnectedAsync(ClientDto client);
        Task NotifyClientDisconnectedAsync(string clientId);
        Task NotifyClientStatusChangedAsync(ClientDto client);

        // Dashboard updates
        Task RefreshDashboardStatsAsync();
        Task RefreshClientListAsync();
        Task RefreshAlarmListAsync();

        // Connection info
        Task<int> GetConnectedUsersCountAsync();
    }
}