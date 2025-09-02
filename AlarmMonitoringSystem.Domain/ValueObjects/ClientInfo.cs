using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Domain.ValueObjects
{
    public record ClientInfo
    {
        public string ClientId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string IpAddress { get; init; } = string.Empty;
        public int Port { get; init; }
        public ConnectionStatus Status { get; init; }
        public DateTime? LastConnectedAt { get; init; }
        public DateTime? LastDisconnectedAt { get; init; }
        public bool IsActive { get; init; }

        public static ClientInfo Create(
            string clientId,
            string name,
            string ipAddress,
            int port,
            string? description = null,
            ConnectionStatus status = ConnectionStatus.Disconnected,
            bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("ClientId cannot be null or empty", nameof(clientId));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IpAddress cannot be null or empty", nameof(ipAddress));

            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

            return new ClientInfo
            {
                ClientId = clientId.Trim(),
                Name = name.Trim(),
                Description = description?.Trim(),
                IpAddress = ipAddress.Trim(),
                Port = port,
                Status = status,
                IsActive = isActive
            };
        }

        public ClientInfo WithStatus(ConnectionStatus newStatus)
        {
            var now = DateTime.UtcNow;
            return this with
            {
                Status = newStatus,
                LastConnectedAt = newStatus == ConnectionStatus.Connected ? now : LastConnectedAt,
                LastDisconnectedAt = newStatus == ConnectionStatus.Disconnected ? now : LastDisconnectedAt
            };
        }
    }
}
