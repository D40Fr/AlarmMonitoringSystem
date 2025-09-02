using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Infrastructure.Data.Repositories;
using AlarmMonitoringSystem.Infrastructure.Data.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmMonitoringSystem.Infrastructure.Data.Extensions
{
    public static class RepositoryExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Register repositories
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IAlarmRepository, AlarmRepository>();
            services.AddScoped<IConnectionLogRepository, ConnectionLogRepository>();

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWorkk>();

            return services;
        }
    }
}
