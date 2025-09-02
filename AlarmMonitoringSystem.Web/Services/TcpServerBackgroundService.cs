using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Infrastructure.TcpServer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Web.Services
{
    public class TcpServerBackgroundService : BackgroundService
    {
        private readonly ITcpServerService _tcpServerService;
        private readonly ILogger<TcpServerBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public TcpServerBackgroundService(
            ITcpServerService tcpServerService,
            ILogger<TcpServerBackgroundService> logger,
            IConfiguration configuration)
        {
            _tcpServerService = tcpServerService;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("TCP Server Background Service is starting...");

                // Get port from configuration (default to 6060)
                var port = _configuration.GetValue<int>("TcpServer:Port", 6060);

                // Wait a moment for other services to initialize
                await Task.Delay(2000, stoppingToken);

                // Start the TCP server
                await _tcpServerService.StartAsync(port, stoppingToken);

                _logger.LogInformation("TCP Server started successfully on port {Port}", port);

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Perform periodic maintenance
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                    // You can add periodic health checks here if needed
                    if (_tcpServerService.IsRunning)
                    {
                        var connectedCount = await _tcpServerService.GetConnectedClientCountAsync(stoppingToken);
                        _logger.LogDebug("TCP Server status - Connected clients: {Count}", connectedCount);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TCP Server Background Service is stopping due to cancellation...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP Server Background Service encountered an error");
                throw; // This will restart the service
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TCP Server Background Service is stopping...");

            try
            {
                if (_tcpServerService.IsRunning)
                {
                    await _tcpServerService.StopAsync(cancellationToken);
                    _logger.LogInformation("TCP Server stopped successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TCP Server");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}