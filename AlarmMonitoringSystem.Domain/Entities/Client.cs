using AlarmMonitoringSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Domain.Entities
{
    public class Client : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(45)] // IPv6 max length
        public string IpAddress { get; set; } = string.Empty;

        public int Port { get; set; }

        public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;

        public DateTime? LastConnectedAt { get; set; }

        public DateTime? LastDisconnectedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
        public virtual ICollection<ConnectionLog> ConnectionLogs { get; set; } = new List<ConnectionLog>();
    }
}
