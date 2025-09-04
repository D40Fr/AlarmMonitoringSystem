// AlarmMonitoringSystem.Web/Services/ConnectionCleanupBackgroundService.cs
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Web.Services
{
    public class ConnectionCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConnectionCleanupBackgroundService> _logger;

        public ConnectionCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ConnectionCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Connection Cleanup Background Service is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Run cleanup every 30 seconds
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                    _logger.LogDebug("Running connection cleanup tasks...");

                    // Create a scope to get scoped services
                    using var scope = _serviceProvider.CreateScope();
                    var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
                    var connectionLogService = scope.ServiceProvider.GetRequiredService<IConnectionLogService>();

                    // Get all clients that are marked as connected but haven't been active
                    var connectedClients = await clientService.GetClientsByStatusAsync(
                        Domain.Enums.ConnectionStatus.Connected, stoppingToken);

                    foreach (var client in connectedClients)
                    {
                        // Check if client has been inactive for more than 5 minutes
                        var lastLog = await connectionLogService.GetLastConnectionLogAsync(client.Id, stoppingToken);

                        if (lastLog != null &&
                            lastLog.Status == Domain.Enums.ConnectionStatus.Connected &&
                            DateTime.UtcNow - lastLog.LogTime > TimeSpan.FromMinutes(5))
                        {
                            _logger.LogWarning("Client {ClientId} ({ClientName}) appears to be dead - no activity for {Minutes} minutes",
                                client.ClientId, client.Name, (DateTime.UtcNow - lastLog.LogTime).TotalMinutes);

                            // Update status to disconnected
                            await clientService.UpdateClientStatusAsync(client.Id,
                                Domain.Enums.ConnectionStatus.Disconnected, stoppingToken);

                            // Log the cleanup disconnection
                            await connectionLogService.LogClientDisconnectedAsync(client.Id,
                                "Connection cleanup - client appears dead", stoppingToken);

                            _logger.LogInformation("Updated dead client {ClientId} status to Disconnected", client.ClientId);
                        }
                    }

                    _logger.LogDebug("Connection cleanup tasks completed successfully");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Connection cleanup service cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during connection cleanup tasks");
                    // Continue running even if cleanup fails
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connection Cleanup Background Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}