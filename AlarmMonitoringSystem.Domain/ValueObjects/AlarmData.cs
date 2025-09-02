using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Domain.ValueObjects
{
    public record AlarmData
    {
        public string AlarmId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public AlarmType Type { get; init; }
        public AlarmSeverity Severity { get; init; }
        public DateTime AlarmTime { get; init; } = DateTime.UtcNow;
        public string? Zone { get; init; }
        public decimal? NumericValue { get; init; }
        public string? Unit { get; init; }
        public Dictionary<string, object>? AdditionalData { get; init; }

        public static AlarmData Create(
            string alarmId,
            string title,
            string message,
            AlarmType type,
            AlarmSeverity severity,
            DateTime? alarmTime = null,
            string? zone = null,
            decimal? numericValue = null,
            string? unit = null,
            Dictionary<string, object>? additionalData = null)
        {
            if (string.IsNullOrWhiteSpace(alarmId))
                throw new ArgumentException("AlarmId cannot be null or empty", nameof(alarmId));

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be null or empty", nameof(title));

            return new AlarmData
            {
                AlarmId = alarmId.Trim(),
                Title = title.Trim(),
                Message = message?.Trim() ?? string.Empty,
                Type = type,
                Severity = severity,
                AlarmTime = alarmTime ?? DateTime.UtcNow,
                Zone = zone?.Trim(),
                NumericValue = numericValue,
                Unit = unit?.Trim(),
                AdditionalData = additionalData
            };
        }
    }
}
