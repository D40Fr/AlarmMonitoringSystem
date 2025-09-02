using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Domain.Enums
{
    public enum ConnectionStatus
    {
        Disconnected = 0,
        Connected = 1,
        Connecting = 2,
        Error = 3,
        Timeout = 4
    }
}
