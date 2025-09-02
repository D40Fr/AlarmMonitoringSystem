using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Domain.Enums
{
    public enum AlarmType
    {
        Temperature = 1,
        Pressure = 2,
        Voltage = 3,
        Current = 4,
        Motion = 5,
        Door = 6,
        System = 7,
        Network = 8,
        Security = 9,
        Other = 99
    }
}
