using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.Services;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

            // Register AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            // Register FluentValidation
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}