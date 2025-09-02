using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;


namespace AlarmMonitoringSystem.Application.DTOs
{
    public class ClientDto
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public ConnectionStatus Status { get; set; }
        public DateTime? LastConnectedAt { get; set; }
        public DateTime? LastDisconnectedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Optional: Basic statistics (can be populated when needed)
        public int ActiveAlarmCount { get; set; }
        public DateTime? LastAlarmTime { get; set; }
        // Display helpers
        public string StatusDisplay => Status switch
        {
            ConnectionStatus.Connected => "Connected",
            ConnectionStatus.Disconnected => "Disconnected",
            ConnectionStatus.Connecting => "Connecting",
            ConnectionStatus.Error => "Error",
            ConnectionStatus.Timeout => "Timeout",
            _ => "Unknown"
        };

        public string StatusCssClass => Status switch
        {
            ConnectionStatus.Connected => "text-success",
            ConnectionStatus.Disconnected => "text-secondary",
            ConnectionStatus.Connecting => "text-warning",
            ConnectionStatus.Error => "text-danger",
            ConnectionStatus.Timeout => "text-warning",
            _ => "text-muted"
        };
    }
}