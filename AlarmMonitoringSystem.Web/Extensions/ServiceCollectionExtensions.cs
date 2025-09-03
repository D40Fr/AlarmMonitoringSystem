// AlarmMonitoringSystem.Web/Extensions/ServiceCollectionExtensions.cs
using AlarmMonitoringSystem.Application.Extensions;
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

            // Add application services
            services.AddApplicationServices();

            // Add TCP server configuration
            var tcpConfig = new TcpServerConfiguration();
            configuration.GetSection("TcpServer").Bind(tcpConfig);
            services.AddSingleton(tcpConfig);

            // ✅ FIXED: Register TCP server service properly
            services.AddSingleton<ITcpServerService, TcpServerService>();

            // Add background services
            services.AddHostedService<TcpServerBackgroundService>();
            services.AddHostedService<MaintenanceBackgroundService>();

            // Add logging
            services.AddLogging();

            return services;
        }
    }
}