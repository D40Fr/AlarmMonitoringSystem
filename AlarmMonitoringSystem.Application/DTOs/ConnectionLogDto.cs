using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;


namespace AlarmMonitoringSystem.Application.DTOs
{
    public class ConnectionLogDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientIdentifier { get; set; } = string.Empty;
        public ConnectionStatus Status { get; set; }
        public string? Message { get; set; }
        public DateTime LogTime { get; set; }
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }

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



        public string StatusIcon => Status switch
        {
            ConnectionStatus.Connected => "fas fa-check-circle text-success",
            ConnectionStatus.Disconnected => "fas fa-times-circle text-secondary",
            ConnectionStatus.Connecting => "fas fa-spinner fa-spin text-warning",
            ConnectionStatus.Error => "fas fa-exclamation-triangle text-danger",
            ConnectionStatus.Timeout => "fas fa-clock text-warning",
            _ => "fas fa-question-circle text-muted"
        };

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - LogTime;
                return diff.TotalMinutes switch
                {
                    < 1 => "Just now",
                    < 60 => $"{(int)diff.TotalMinutes} min ago",
                    < 1440 => $"{(int)diff.TotalHours} hour(s) ago",
                    _ => $"{(int)diff.TotalDays} day(s) ago"
                };
            }
        }
    }
}