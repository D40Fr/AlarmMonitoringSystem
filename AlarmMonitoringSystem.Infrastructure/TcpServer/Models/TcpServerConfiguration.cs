using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Infrastructure.TcpServer.Models
{
    public class TcpServerConfiguration
    {
        public int Port { get; set; } = 6060;
        public string IpAddress { get; set; } = "0.0.0.0"; // Listen on all interfaces
        public int MaxConnections { get; set; } = 100;
        public int BufferSize { get; set; } = 1024;
        public int ConnectionTimeoutSeconds { get; set; } = 300; // 5 minutes
        public int HeartbeatIntervalSeconds { get; set; } = 30;
        public bool EnableHeartbeat { get; set; } = true;
        public int MaxMessageSize { get; set; } = 10240; // 10KB
        public bool LogRawMessages { get; set; } = false; // For debugging
    }
}
