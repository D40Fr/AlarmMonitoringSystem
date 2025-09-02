using AlarmMonitoringSystem.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Web.Services
{
    public class MaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MaintenanceBackgroundService> _logger;

        public MaintenanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MaintenanceBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Maintenance Background Service is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run maintenance every 30 minutes
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

                    _logger.LogInformation("Running maintenance tasks...");

                    // Create a scope to get scoped services
                    using var scope = _serviceProvider.CreateScope();
                    var connectionLogService = scope.ServiceProvider.GetRequiredService<IConnectionLogService>();

                    // Cleanup old connection logs (keep last 30 days)
                    await connectionLogService.CleanupOldLogsAsync(TimeSpan.FromDays(30), stoppingToken);

                    // You can add more maintenance tasks here:
                    // - Cleanup old acknowledged alarms
                    // - Update client statistics
                    // - Health checks
                    // - Database optimization

                    _logger.LogInformation("Maintenance tasks completed successfully");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Maintenance service cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during maintenance tasks");
                    // Continue running even if maintenance fails
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Maintenance Background Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}