using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.ValueObjects;
using System.Net;

namespace AlarmMonitoringSystem.Domain.Interfaces.Services
{
    public interface ITcpServerService
    {
        // Server lifecycle
        Task StartAsync(int port, CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        bool IsRunning { get; }
        int? Port { get; }

        // Client connection management
        Task<bool> DisconnectClientAsync(string clientId, CancellationToken cancellationToken = default);
        Task<bool> DisconnectClientAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task DisconnectAllClientsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetConnectedClientIdsAsync(CancellationToken cancellationToken = default);
        Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default);

        // Message handling
        Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default);
        Task SendMessageToClientAsync(string clientId, string message, CancellationToken cancellationToken = default);
        Task SendMessageToClientAsync(Guid clientId, string message, CancellationToken cancellationToken = default);

        // Events
        event Func<ClientInfo, Task> ClientConnected;
        event Func<string, string?, Task> ClientDisconnected; // clientId, reason
        event Func<string, AlarmData, Task> AlarmReceived; // clientId, alarmData
        event Func<string, string, Exception?, Task> ErrorOccurred; // clientId, message, exception

        // Server information
        Task<Dictionary<string, object>> GetServerStatusAsync(CancellationToken cancellationToken = default);
        Task<TimeSpan> GetUptimeAsync(CancellationToken cancellationToken = default);
        Task<long> GetTotalMessagesReceivedAsync(CancellationToken cancellationToken = default);
        Task<long> GetTotalMessagesProcessedAsync(CancellationToken cancellationToken = default);
    }
}
