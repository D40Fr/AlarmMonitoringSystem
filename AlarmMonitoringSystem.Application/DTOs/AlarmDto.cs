using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Application.DTOs
{
    public class AlarmDto
    {
        public Guid Id { get; set; }
        public string AlarmId { get; set; } = string.Empty;
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientIdentifier { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlarmType Type { get; set; }
        public AlarmSeverity Severity { get; set; }
        public DateTime AlarmTime { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public bool IsActive { get; set; }
        public string? Zone { get; set; }
        public decimal? NumericValue { get; set; }
        public string? Unit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Display helpers
        public string SeverityDisplay => Severity switch
        {
            AlarmSeverity.Low => "Low",
            AlarmSeverity.Medium => "Medium",
            AlarmSeverity.High => "High",
            AlarmSeverity.Critical => "Critical",
            _ => "Unknown"
        };

        public string SeverityBadgeClass => Severity switch
        {
            AlarmSeverity.Low => "badge-info",
            AlarmSeverity.Medium => "badge-warning",
            AlarmSeverity.High => "badge-danger",
            AlarmSeverity.Critical => "badge-dark",
            _ => "badge-secondary"
        };

        public string TypeDisplay => Type switch
        {
            AlarmType.Temperature => "Temperature",
            AlarmType.Pressure => "Pressure",
            AlarmType.Voltage => "Voltage",
            AlarmType.Current => "Current",
            AlarmType.Motion => "Motion",
            AlarmType.Door => "Door",
            AlarmType.System => "System",
            AlarmType.Network => "Network",
            AlarmType.Security => "Security",
            AlarmType.Other => "Other",
            _ => "Unknown"
        };

        public string FormattedValue => NumericValue.HasValue && !string.IsNullOrEmpty(Unit)
            ? $"{NumericValue:F2} {Unit}"
            : NumericValue?.ToString("F2") ?? "N/A";

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow - AlarmTime;
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