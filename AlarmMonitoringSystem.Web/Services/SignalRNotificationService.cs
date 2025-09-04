// AlarmMonitoringSystem.Web/Services/SignalRNotificationService.cs
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Application.Interfaces; // ✅ FIX: Implement Application interface
using AlarmMonitoringSystem.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AlarmMonitoringSystem.Web.Services
{
    public class SignalRNotificationService : IRealtimeNotificationService // ✅ FIX: Renamed interface
    {
        private readonly IHubContext<AlarmMonitoringHub> _hubContext;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<AlarmMonitoringHub> hubContext,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        // Alarm notifications
        public async Task NotifyNewAlarmAsync(AlarmDto alarm)
        {
            try
            {
                _logger.LogInformation("Broadcasting new alarm: {AlarmId}", alarm.AlarmId);

                await _hubContext.Clients.Groups("Dashboard", "Alarms").SendAsync("NewAlarm", alarm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting new alarm {AlarmId}", alarm.AlarmId);
            }
        }

        public async Task NotifyAlarmAcknowledgedAsync(Guid alarmId, string acknowledgedBy)
        {
            try
            {
                _logger.LogInformation("Broadcasting alarm acknowledged: {AlarmId}", alarmId);

                var data = new { AlarmId = alarmId, AcknowledgedBy = acknowledgedBy, AcknowledgedAt = DateTime.UtcNow };
                await _hubContext.Clients.Groups("Dashboard", "Alarms").SendAsync("AlarmAcknowledged", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting alarm acknowledgment {AlarmId}", alarmId);
            }
        }

        // Client status notifications
        public async Task NotifyClientConnectedAsync(ClientDto client)
        {
            try
            {
                _logger.LogInformation("Broadcasting client connected: {ClientId}", client.ClientId);

                await _hubContext.Clients.Groups("Dashboard", "Clients").SendAsync("ClientConnected", client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting client connection {ClientId}", client.ClientId);
            }
        }

        public async Task NotifyClientDisconnectedAsync(string clientId)
        {
            try
            {
                _logger.LogInformation("Broadcasting client disconnected: {ClientId}", clientId);

                var data = new { ClientId = clientId, DisconnectedAt = DateTime.UtcNow };
                await _hubContext.Clients.Groups("Dashboard", "Clients").SendAsync("ClientDisconnected", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting client disconnection {ClientId}", clientId);
            }
        }

        public async Task NotifyClientStatusChangedAsync(ClientDto client)
        {
            try
            {
                _logger.LogInformation("Broadcasting client status change: {ClientId} - {Status}", client.ClientId, client.Status);

                await _hubContext.Clients.Groups("Dashboard", "Clients").SendAsync("ClientStatusChanged", client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting client status change {ClientId}", client.ClientId);
            }
        }

        // Dashboard updates
        public async Task RefreshDashboardStatsAsync()
        {
            try
            {
                _logger.LogDebug("Broadcasting dashboard stats refresh");

                // Send a dedicated refresh message to the dashboard
                await _hubContext.Clients.Group("Dashboard").SendAsync("RefreshDashboard");

                // Also send the usual RefreshStats message
                await _hubContext.Clients.Group("Dashboard").SendAsync("RefreshStats");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting dashboard refresh");
            }
        }

        public async Task RefreshClientListAsync()
        {
            try
            {
                _logger.LogDebug("Broadcasting client list refresh");

                await _hubContext.Clients.Group("Clients").SendAsync("RefreshClientList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting client list refresh");
            }
        }

        public async Task RefreshAlarmListAsync()
        {
            try
            {
                _logger.LogDebug("Broadcasting alarm list refresh");

                await _hubContext.Clients.Group("Alarms").SendAsync("RefreshAlarmList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting alarm list refresh");
            }
        }

        // Connection info
        public async Task<int> GetConnectedUsersCountAsync()
        {
            try
            {
                // Simple implementation for basic project
                return await Task.FromResult(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connected users count");
                return 0;
            }
        }

        // Connection log notifications
        public async Task NotifyConnectionLogAddedAsync()
        {
            try
            {
                _logger.LogDebug("Broadcasting connection log update");
                
                // Send refresh message to all connected clients on the dashboard
                await _hubContext.Clients.Group("Dashboard").SendAsync("RefreshDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting connection log update");
            }
        }
    }
}