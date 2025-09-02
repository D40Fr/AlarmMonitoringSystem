using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace AlarmMonitoringSystem.Infrastructure.Data.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            // Table configuration
            builder.ToTable("Clients");

            // Primary key
            builder.HasKey(c => c.Id);

            // Properties
            builder.Property(c => c.Id)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.ClientId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.Property(c => c.IpAddress)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length

            builder.Property(c => c.Port)
                .IsRequired();

            builder.Property(c => c.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(ConnectionStatus.Disconnected);

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            // Indexes
            builder.HasIndex(c => c.ClientId)
                .IsUnique()
                .HasDatabaseName("IX_Clients_ClientId");

            builder.HasIndex(c => new { c.IpAddress, c.Port })
                .HasDatabaseName("IX_Clients_IpAddress_Port");

            builder.HasIndex(c => c.Status)
                .HasDatabaseName("IX_Clients_Status");

            builder.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Clients_IsActive");

            // Relationships
            builder.HasMany(c => c.Alarms)
                .WithOne(a => a.Client)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.ConnectionLogs)
                .WithOne(cl => cl.Client)
                .HasForeignKey(cl => cl.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
