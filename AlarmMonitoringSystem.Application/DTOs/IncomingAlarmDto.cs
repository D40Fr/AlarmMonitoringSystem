using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Application.DTOs
{
    /// <summary>
    /// DTO for parsing JSON alarm data from TCP clients
    /// </summary>
    public class IncomingAlarmDto
    {
        [JsonPropertyName("alarmId")]
        public string AlarmId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        [JsonPropertyName("zone")]
        public string? Zone { get; set; }

        [JsonPropertyName("value")]
        public decimal? Value { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }
        [JsonPropertyName("additionalData")]
        public Dictionary<string, object>? AdditionalData { get; set; }

        // Helper methods to convert string enums to domain enums
        public AlarmType GetAlarmType()
        {
            return Type.ToLowerInvariant() switch
            {
                "temperature" => AlarmType.Temperature,
                "pressure" => AlarmType.Pressure,
                "voltage" => AlarmType.Voltage,
                "current" => AlarmType.Current,
                "motion" => AlarmType.Motion,
                "door" => AlarmType.Door,
                "system" => AlarmType.System,
                "network" => AlarmType.Network,
                "security" => AlarmType.Security,
                _ => AlarmType.Other
            };
        }

        public AlarmSeverity GetAlarmSeverity()
        {
            return Severity.ToLowerInvariant() switch
            {
                "low" => AlarmSeverity.Low,
                "medium" => AlarmSeverity.Medium,
                "high" => AlarmSeverity.High,
                "critical" => AlarmSeverity.Critical,
                _ => AlarmSeverity.Medium // Default to medium if unknown
            };
        }
    }
}

