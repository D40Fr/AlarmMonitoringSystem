using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AlarmMonitoringSystem.Domain.Entities
{
    public class Alarm : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string AlarmId { get; set; } = string.Empty;

        [Required]
        public Guid ClientId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public AlarmType Type { get; set; }

        public AlarmSeverity Severity { get; set; }

        public DateTime AlarmTime { get; set; } = DateTime.UtcNow;

        public bool IsAcknowledged { get; set; } = false;

        public DateTime? AcknowledgedAt { get; set; }

        [MaxLength(100)]
        public string? AcknowledgedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // Additional properties for alarm data
        [MaxLength(50)]
        public string? Zone { get; set; }

        public decimal? NumericValue { get; set; }

        [MaxLength(20)]
        public string? Unit { get; set; }

        // Raw JSON data from client
        public string? RawData { get; set; }

        // Navigation property
        public virtual Client Client { get; set; } = null!;
    }
}
