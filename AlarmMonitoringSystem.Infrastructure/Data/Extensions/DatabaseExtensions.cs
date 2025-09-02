using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Infrastructure.Data.Extensions
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AlarmMonitoringDbContext>(options =>
            {
                options.UseSqlite(connectionString);

                // Enable sensitive data logging in development
                options.EnableSensitiveDataLogging();

                // Enable detailed errors
                options.EnableDetailedErrors();

                // Configure query splitting behavior
                //options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);  EF Core 9.0'da bu method yok, claudeun önerdiği eski versiyonlarla dönebilirim daha fazla sorun olacaksa
            });

            return services;
        }

        public static async Task<IHost> MigrateDbAsync(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<AlarmMonitoringDbContext>>();
            var context = services.GetRequiredService<AlarmMonitoringDbContext>();

            try
            {
                logger.LogInformation("Starting database migration...");

                // Ensure database is created and apply any pending migrations
                await context.Database.MigrateAsync();

                // Seed initial data if needed
                await SeedInitialDataAsync(context, logger);

                logger.LogInformation("Database migration completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database.");
                throw;
            }

            return host;
        }

        private static async Task SeedInitialDataAsync(AlarmMonitoringDbContext context, ILogger logger)
        {
            try
            {
                // Check if data already exists
                if (await context.Clients.AnyAsync())
                {
                    logger.LogInformation("Database already contains data. Skipping seed.");
                    return;
                }

                logger.LogInformation("Seeding initial data...");

                // You can add initial data here if needed
                // For example, default clients or configuration data

                await context.SaveChangesAsync();
                logger.LogInformation("Initial data seeding completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding initial data.");
                throw;
            }
        }

        public static async Task<bool> CanConnectAsync(this AlarmMonitoringDbContext context)
        {
            try
            {
                return await context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> EnsureCreatedAsync(this AlarmMonitoringDbContext context)
        {
            try
            {
                return await context.Database.EnsureCreatedAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
