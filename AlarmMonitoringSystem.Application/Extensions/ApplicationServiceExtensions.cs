// AlarmMonitoringSystem.Application/Extensions/ApplicationServiceExtensions.cs
using AlarmMonitoringSystem.Application.Mappers;
using AlarmMonitoringSystem.Application.Services;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Application.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register Application Services
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IAlarmService, AlarmService>();
            services.AddScoped<IConnectionLogService, ConnectionLogService>();
            services.AddScoped<ITcpMessageProcessorService, TcpMessageProcessorService>();

            // ✅ FIXED: AutoMapper 15.x with logger factory parameter
            services.AddSingleton<IMapper>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<AlarmMappingProfile>();
                    cfg.AddProfile<ClientMappingProfile>();
                    cfg.AddProfile<ConnectionLogMappingProfile>();
                }, loggerFactory);

                return config.CreateMapper();
            });

            // Register FluentValidation
            services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

            return services;
        }
    }
}