using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
/*
    dotnet tool install --global dotnet-ef
    dotnet ef migrations add InitialCreate --project AlarmMonitoringSystem.Infrastructure --startup-project AlarmMonitoringSystem.WebApi
*/
namespace AlarmMonitoringSystem.Infrastructure.Data.Context
{
    public class AlarmMonitoringDbContext : DbContext
    {
        public AlarmMonitoringDbContext(DbContextOptions<AlarmMonitoringDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Client> Clients { get; set; }
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<ConnectionLog> ConnectionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations from current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Apply custom configurations
            modelBuilder.ApplyConfiguration(new ClientConfiguration());
            modelBuilder.ApplyConfiguration(new AlarmConfiguration());
            modelBuilder.ApplyConfiguration(new ConnectionLogConfiguration());

            // Set default values for timestamps
            SetTimestampDefaults(modelBuilder);
        }

        private static void SetTimestampDefaults(ModelBuilder modelBuilder)
        {
            // Set default values for CreatedAt
            modelBuilder.Entity<Client>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Alarm>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<ConnectionLog>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}
