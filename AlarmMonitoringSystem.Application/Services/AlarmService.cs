// AlarmMonitoringSystem.Application/Services/AlarmService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Application.Interfaces; // ✅ FIX: Use Application layer interface
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Domain.ValueObjects;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Application.Services
{
    public class AlarmService : IAlarmService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IRealtimeNotificationService _realtimeNotificationService; // ✅ FIX: Use Application layer interface
        private readonly ILogger<AlarmService> _logger;

        public AlarmService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRealtimeNotificationService realtimeNotificationService, // ✅ FIX: Use Application layer interface
            ILogger<AlarmService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _realtimeNotificationService = realtimeNotificationService; // ✅ FIX: Use Application layer interface
            _logger = logger;
        }

        public async Task<Alarm> ProcessAlarmAsync(Guid clientId, AlarmData alarmData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing alarm {AlarmId} for client {ClientId}", alarmData.AlarmId, clientId);

            // Check for duplicate alarm (CRITICAL REQUIREMENT)
            var isDuplicate = await _unitOfWork.Alarms.AlarmExistsAsync(alarmData.AlarmId, clientId, cancellationToken);
            if (isDuplicate)
            {
                _logger.LogWarning("Duplicate alarm detected: {AlarmId} for client {ClientId}", alarmData.AlarmId, clientId);
                throw new InvalidOperationException($"Alarm '{alarmData.AlarmId}' already exists for this client.");
            }

            // Verify client exists
            var client = await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken);
            if (client == null)
            {
                _logger.LogError("Client {ClientId} not found for alarm {AlarmId}", clientId, alarmData.AlarmId);
                throw new InvalidOperationException($"Client with ID '{clientId}' not found.");
            }

            // Create alarm entity
            var alarm = new Alarm
            {
                AlarmId = alarmData.AlarmId,
                ClientId = clientId,
                Title = alarmData.Title,
                Message = alarmData.Message,
                Type = alarmData.Type,
                Severity = alarmData.Severity,
                AlarmTime = alarmData.AlarmTime,
                NumericValue = alarmData.NumericValue,
                Unit = alarmData.Unit,
                IsActive = true,
                IsAcknowledged = false
            };

            await _unitOfWork.Alarms.AddAsync(alarm, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alarm {AlarmId} processed successfully for client {ClientId}", alarmData.AlarmId, clientId);
            return alarm;
        }

        public async Task<Alarm> ProcessAlarmAsync(string clientId, AlarmData alarmData, CancellationToken cancellationToken = default)
        {
            var client = await _unitOfWork.Clients.GetByClientIdAsync(clientId, cancellationToken);
            if (client == null)
            {
                _logger.LogError("Client {ClientId} not found for alarm {AlarmId}", clientId, alarmData.AlarmId);
                throw new InvalidOperationException($"Client with ID '{clientId}' not found.");
            }

            return await ProcessAlarmAsync(client.Id, alarmData, cancellationToken);
        }

        public async Task<bool> IsAlarmDuplicateAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.AlarmExistsAsync(alarmId, clientId, cancellationToken);
        }

        public async Task<Alarm?> GetAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetByIdAsync(alarmId, cancellationToken);
        }

        public async Task<Alarm?> GetAlarmByAlarmIdAsync(string alarmId, Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetByAlarmIdAsync(alarmId, clientId, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetClientAlarmsAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetByClientIdAsync(clientId, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetAlarmsBySeverityAsync(AlarmSeverity severity, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetBySeverityAsync(severity, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetAlarmsByTypeAsync(AlarmType type, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetByTypeAsync(type, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetActiveAlarmsAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetUnacknowledgedAlarmsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetUnacknowledgedAlarmsAsync(cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> GetRecentAlarmsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetRecentAlarmsAsync(count, cancellationToken);
        }

        // ✅ FIX: Add notification for alarm acknowledgment
        public async Task<Alarm> AcknowledgeAlarmAsync(Guid alarmId, string acknowledgedBy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Acknowledging alarm {AlarmId} by {User}", alarmId, acknowledgedBy);

            var alarm = await _unitOfWork.Alarms.GetByIdAsync(alarmId, cancellationToken);
            if (alarm == null)
            {
                throw new InvalidOperationException($"Alarm with ID '{alarmId}' not found.");
            }

            if (alarm.IsAcknowledged)
            {
                _logger.LogWarning("Alarm {AlarmId} is already acknowledged", alarmId);
                return alarm;
            }

            await _unitOfWork.Alarms.AcknowledgeAlarmAsync(alarmId, acknowledgedBy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ✅ FIX: Broadcast alarm acknowledgment via notification service
            try
            {
                await _realtimeNotificationService.NotifyAlarmAcknowledgedAsync(alarmId, acknowledgedBy);
                _logger.LogInformation("Successfully broadcasted alarm acknowledgment {AlarmId} via notifications", alarmId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast alarm acknowledgment {AlarmId} via notifications", alarmId);
                // Don't fail the operation if notification broadcast fails
            }

            // Get updated alarm
            var updatedAlarm = await _unitOfWork.Alarms.GetByIdAsync(alarmId, cancellationToken);
            _logger.LogInformation("Alarm {AlarmId} acknowledged by {User}", alarmId, acknowledgedBy);
            return updatedAlarm!;
        }

        // ... rest of the methods remain the same
        public async Task<Alarm> DeactivateAlarmAsync(Guid alarmId, CancellationToken cancellationToken = default)
        {
            var alarm = await _unitOfWork.Alarms.GetByIdAsync(alarmId, cancellationToken);
            if (alarm == null)
            {
                throw new InvalidOperationException($"Alarm with ID '{alarmId}' not found.");
            }

            alarm.IsActive = false;
            await _unitOfWork.Alarms.UpdateAsync(alarm, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Alarm {AlarmId} deactivated", alarmId);
            return alarm;
        }

        public async Task<IEnumerable<Alarm>> GetAlarmsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.GetAlarmsByDateRangeAsync(startDate, endDate, cancellationToken);
        }

        public async Task<IEnumerable<Alarm>> SearchAlarmsAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.SearchAlarmsAsync(searchTerm, cancellationToken);
        }

        public async Task<int> GetAlarmCountAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Alarms.CountAsync(cancellationToken);
        }

        public async Task<int> GetActiveAlarmCountAsync(CancellationToken cancellationToken = default)
        {
            var activeAlarms = await _unitOfWork.Alarms.GetActiveAlarmsAsync(cancellationToken);
            return activeAlarms.Count();
        }

        public async Task<int> GetUnacknowledgedAlarmCountAsync(CancellationToken cancellationToken = default)
        {
            var unacknowledgedAlarms = await _unitOfWork.Alarms.GetUnacknowledgedAlarmsAsync(cancellationToken);
            return unacknowledgedAlarms.Count();
        }

        public async Task<Dictionary<AlarmSeverity, int>> GetAlarmCountsBySeverityAsync(CancellationToken cancellationToken = default)
        {
            var activeAlarms = await _unitOfWork.Alarms.GetActiveAlarmsAsync(cancellationToken);
            return activeAlarms
                .GroupBy(a => a.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<Dictionary<AlarmType, int>> GetAlarmCountsByTypeAsync(CancellationToken cancellationToken = default)
        {
            var activeAlarms = await _unitOfWork.Alarms.GetActiveAlarmsAsync(cancellationToken);
            return activeAlarms
                .GroupBy(a => a.Type)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}