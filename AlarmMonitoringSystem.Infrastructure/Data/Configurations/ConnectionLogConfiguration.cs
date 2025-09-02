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
    public class ConnectionLogConfiguration : IEntityTypeConfiguration<ConnectionLog>
    {
        public void Configure(EntityTypeBuilder<ConnectionLog> builder)
        {
            // Table configuration
            builder.ToTable("ConnectionLogs");

            // Primary key
            builder.HasKey(cl => cl.Id);

            // Properties
            builder.Property(cl => cl.Id)
                .ValueGeneratedOnAdd();

            builder.Property(cl => cl.ClientId)
                .IsRequired();

            builder.Property(cl => cl.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(cl => cl.Message)
                .HasMaxLength(500);

            builder.Property(cl => cl.LogLevel)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(LogLevel.Information);

            builder.Property(cl => cl.LogTime)
                .IsRequired();

            builder.Property(cl => cl.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            builder.Property(cl => cl.Details)
                .HasMaxLength(1000);

            builder.Property(cl => cl.CreatedAt)
                .IsRequired();

            // Indexes for performance
            builder.HasIndex(cl => cl.ClientId)
                .HasDatabaseName("IX_ConnectionLogs_ClientId");

            builder.HasIndex(cl => cl.LogTime)
                .HasDatabaseName("IX_ConnectionLogs_LogTime");

            builder.HasIndex(cl => cl.Status)
                .HasDatabaseName("IX_ConnectionLogs_Status");

            builder.HasIndex(cl => cl.LogLevel)
                .HasDatabaseName("IX_ConnectionLogs_LogLevel");

            builder.HasIndex(cl => new { cl.ClientId, cl.LogTime })
                .HasDatabaseName("IX_ConnectionLogs_ClientId_LogTime");

            // Relationship
            builder.HasOne(cl => cl.Client)
                .WithMany(c => c.ConnectionLogs)
                .HasForeignKey(cl => cl.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
