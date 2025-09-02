using AlarmMonitoringSystem.Domain.Enums;
using System.Net.Sockets;

namespace AlarmMonitoringSystem.Infrastructure.TcpServer.Models
{
    public class TcpClientInfo
    {
        public string ClientId { get; set; } = string.Empty;
        public TcpClient TcpClient { get; set; } = null!;
        public NetworkStream Stream { get; set; } = null!;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Connected;
        public bool IsAuthenticated { get; set; } = false;
        public long MessagesReceived { get; set; } = 0;
        public long MessagesProcessed { get; set; } = 0;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();

        public bool IsConnected => TcpClient?.Connected == true && Status == ConnectionStatus.Connected;

        public TimeSpan ConnectionDuration => DateTime.UtcNow - ConnectedAt;

        public TimeSpan TimeSinceLastActivity => DateTime.UtcNow - LastActivityAt;

        public void UpdateActivity()
        {
            LastActivityAt = DateTime.UtcNow;
        }

        public void Dispose()
        {
            try
            {
                CancellationTokenSource?.Cancel();
                Stream?.Close();
                TcpClient?.Close();
            }
            catch
            {
                // Ignore cleanup errors
            }
            finally
            {
                CancellationTokenSource?.Dispose();
                Stream?.Dispose();
                TcpClient?.Dispose();
            }
        }
    }
}