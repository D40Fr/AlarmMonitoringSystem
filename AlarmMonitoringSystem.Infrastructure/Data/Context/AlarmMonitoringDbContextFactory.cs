using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlarmMonitoringSystem.Infrastructure.Data.Context
{
    // This factory is used by EF Core tools for migrations
    public class AlarmMonitoringDbContextFactory : IDesignTimeDbContextFactory<AlarmMonitoringDbContext>
    {
        public AlarmMonitoringDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AlarmMonitoringDbContext>();

            // Use a default connection string for migrations
            // This will be overridden by the actual application configuration
            optionsBuilder.UseSqlite("Data Source=AlarmMonitoring.db");

            return new AlarmMonitoringDbContext(optionsBuilder.Options);
        }
    }
}
