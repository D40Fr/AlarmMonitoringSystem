using AlarmMonitoringSystem.Infrastructure.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmMonitoringSystem.Infrastructure.Data.Context
{
    public class DbContextHealthCheck : IHealthCheck
    {
        private readonly AlarmMonitoringDbContext _context;

        public DbContextHealthCheck(AlarmMonitoringDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.CanConnectAsync();

                if (canConnect)
                {
                    // Additional checks
                    var clientCount = await _context.Clients.CountAsync(cancellationToken);

                    var data = new Dictionary<string, object>
                    {
                        { "CanConnect", true },
                        { "ClientCount", clientCount },
                        { "DatabaseProvider", _context.Database.ProviderName ?? "Unknown" }
                    };

                    return HealthCheckResult.Healthy("Database is healthy", data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Cannot connect to database");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check failed", ex);
            }
        }
    }
}
