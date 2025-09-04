// AlarmMonitoringSystem.Web/Extensions/ServiceCollectionExtensions.cs
using AlarmMonitoringSystem.Application.Extensions;
using AlarmMonitoringSystem.Application.Interfaces; // ✅ FIX: Use Application interface
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Infrastructure.Data.Extensions;
using AlarmMonitoringSystem.Infrastructure.TcpServer;
using AlarmMonitoringSystem.Infrastructure.TcpServer.Models;
using AlarmMonitoringSystem.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmMonitoringSystem.Web.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAlarmMonitoringServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add database
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=AlarmMonitoring.db";
            services.AddDatabase(connectionString);

            // Add repositories
            services.AddRepositories();

            // Add SignalR services
            services.AddSignalR();

            // Add application services
            services.AddApplicationServices();

            // ✅ FIX: Register realtime notification service with Application interface
            services.AddScoped<IRealtimeNotificationService, SignalRNotificationService>();

            // Add TCP server configuration
            var tcpConfig = new TcpServerConfiguration();
            configuration.GetSection("TcpServer").Bind(tcpConfig);
            services.AddSingleton(tcpConfig);

            // Register TCP server service
            services.AddSingleton<ITcpServerService, TcpServerService>();

            // Add background services
            services.AddHostedService<TcpServerBackgroundService>();
            services.AddHostedService<MaintenanceBackgroundService>();
            services.AddHostedService<ConnectionCleanupBackgroundService>();

            // Add logging
            services.AddLogging();

            return services;
        }
    }
}