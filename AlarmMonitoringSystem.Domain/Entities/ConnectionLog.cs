using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AlarmMonitoringSystem.Domain.Entities
{
    public class ConnectionLog : BaseEntity
    {
        [Required]
        public Guid ClientId { get; set; }

        public ConnectionStatus Status { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        public DateTime LogTime { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public int? Port { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        // Navigation property
        public virtual Client Client { get; set; } = null!;
    }
}
