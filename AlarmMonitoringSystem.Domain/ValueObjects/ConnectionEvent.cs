using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Domain.ValueObjects
{
    public record ConnectionEvent
    {
        public Guid ClientId { get; init; }
        public ConnectionStatus Status { get; init; }
        public DateTime EventTime { get; init; } = DateTime.UtcNow;
        public string? Message { get; init; }
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
        public string? IpAddress { get; init; }
        public int? Port { get; init; }
        public string? Details { get; init; }

        public static ConnectionEvent Create(
            Guid clientId,
            ConnectionStatus status,
            string? message = null,
            LogLevel logLevel = LogLevel.Information,
            string? ipAddress = null,
            int? port = null,
            string? details = null)
        {
            if (clientId == Guid.Empty)
                throw new ArgumentException("ClientId cannot be empty", nameof(clientId));

            return new ConnectionEvent
            {
                ClientId = clientId,
                Status = status,
                EventTime = DateTime.UtcNow,
                Message = message?.Trim(),
                LogLevel = logLevel,
                IpAddress = ipAddress?.Trim(),
                Port = port,
                Details = details?.Trim()
            };
        }
    }
}
