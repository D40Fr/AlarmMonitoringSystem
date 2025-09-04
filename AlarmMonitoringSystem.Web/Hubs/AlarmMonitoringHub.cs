// AlarmMonitoringSystem.Web/Hubs/AlarmMonitoringHub.cs
using Microsoft.AspNetCore.SignalR;

namespace AlarmMonitoringSystem.Web.Hubs
{
    public class AlarmMonitoringHub : Hub
    {
        private readonly ILogger<AlarmMonitoringHub> _logger;

        public AlarmMonitoringHub(ILogger<AlarmMonitoringHub> logger)
        {
            _logger = logger;
        }

        // Client connects to hub
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        // Client disconnects from hub
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // Join dashboard group (for dashboard-specific updates)
        public async Task JoinDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
            _logger.LogDebug("Client {ConnectionId} joined Dashboard group", Context.ConnectionId);
        }

        // Join alarms group (for alarm-specific updates)
        public async Task JoinAlarms()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Alarms");
            _logger.LogDebug("Client {ConnectionId} joined Alarms group", Context.ConnectionId);
        }

        // Join clients group (for client status updates)
        public async Task JoinClients()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Clients");
            _logger.LogDebug("Client {ConnectionId} joined Clients group", Context.ConnectionId);
        }
    }
}