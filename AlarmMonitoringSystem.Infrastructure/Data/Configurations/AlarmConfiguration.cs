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
    public class AlarmConfiguration : IEntityTypeConfiguration<Alarm>
    {
        public void Configure(EntityTypeBuilder<Alarm> builder)
        {
            // Table configuration
            builder.ToTable("Alarms");

            // Primary key
            builder.HasKey(a => a.Id);

            // Properties
            builder.Property(a => a.Id)
                .ValueGeneratedOnAdd();

            builder.Property(a => a.AlarmId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.ClientId)
                .IsRequired();

            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.Message)
                .HasMaxLength(1000);

            builder.Property(a => a.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(a => a.Severity)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(a => a.AlarmTime)
                .IsRequired();

            builder.Property(a => a.IsAcknowledged)
                .HasDefaultValue(false);

            builder.Property(a => a.IsActive)
                .HasDefaultValue(true);


            builder.Property(a => a.NumericValue)
                .HasPrecision(18, 4);

            builder.Property(a => a.Unit)
                .HasMaxLength(20);

            builder.Property(a => a.RawData)
                .HasColumnType("TEXT");

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            // Unique constraint - No duplicate alarms per client
            builder.HasIndex(a => new { a.AlarmId, a.ClientId })
                .IsUnique()
                .HasDatabaseName("IX_Alarms_AlarmId_ClientId_Unique");

            // Other indexes for performance
            builder.HasIndex(a => a.ClientId)
                .HasDatabaseName("IX_Alarms_ClientId");

            builder.HasIndex(a => a.Severity)
                .HasDatabaseName("IX_Alarms_Severity");

            builder.HasIndex(a => a.Type)
                .HasDatabaseName("IX_Alarms_Type");

            builder.HasIndex(a => a.AlarmTime)
                .HasDatabaseName("IX_Alarms_AlarmTime");

            builder.HasIndex(a => a.IsAcknowledged)
                .HasDatabaseName("IX_Alarms_IsAcknowledged");

            builder.HasIndex(a => a.IsActive)
                .HasDatabaseName("IX_Alarms_IsActive");

            builder.HasIndex(a => new { a.IsActive, a.IsAcknowledged })
                .HasDatabaseName("IX_Alarms_IsActive_IsAcknowledged");

            // Relationship
            builder.HasOne(a => a.Client)
                .WithMany(c => c.Alarms)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
