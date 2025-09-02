using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Domain.ValueObjects;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlarmMonitoringSystem.Application.Services
{
    public interface ITcpMessageProcessorService
    {
        Task<bool> ProcessTcpMessageAsync(string clientId, string jsonMessage, CancellationToken cancellationToken = default);
        Task<IncomingAlarmDto?> ParseAlarmMessageAsync(string jsonMessage, CancellationToken cancellationToken = default);
        bool IsValidAlarmMessage(string jsonMessage);
    }

    public class TcpMessageProcessorService : ITcpMessageProcessorService
    {
        private readonly IAlarmService _alarmService;
        private readonly IClientService _clientService;
        private readonly IMapper _mapper;
        private readonly IValidator<IncomingAlarmDto> _validator;
        private readonly ILogger<TcpMessageProcessorService> _logger;

        public TcpMessageProcessorService(
            IAlarmService alarmService,
            IClientService clientService,
            IMapper mapper,
            IValidator<IncomingAlarmDto> validator,
            ILogger<TcpMessageProcessorService> logger)
        {
            _alarmService = alarmService;
            _clientService = clientService;
            _mapper = mapper;
            _validator = validator;
            _logger = logger;
        }

        public async Task<bool> ProcessTcpMessageAsync(string clientId, string jsonMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing TCP message from client {ClientId}", clientId);

                // Parse JSON message
                var incomingAlarm = await ParseAlarmMessageAsync(jsonMessage, cancellationToken);
                if (incomingAlarm == null)
                {
                    _logger.LogWarning("Failed to parse alarm message from client {ClientId}", clientId);
                    return false;
                }

                // Get client information
                var client = await _clientService.GetClientByClientIdAsync(clientId, cancellationToken);
                if (client == null)
                {
                    _logger.LogError("Client {ClientId} not found for alarm processing", clientId);
                    return false;
                }

                // Convert to AlarmData value object
                var alarmData = AlarmData.Create(
                    incomingAlarm.AlarmId,
                    incomingAlarm.Title,
                    incomingAlarm.Message,
                    incomingAlarm.GetAlarmType(),
                    incomingAlarm.GetAlarmSeverity(),
                    incomingAlarm.Timestamp,
                    incomingAlarm.Zone,
                    incomingAlarm.Value,
                    incomingAlarm.Unit,
                    incomingAlarm.AdditionalData);

                // Process alarm through business logic
                await _alarmService.ProcessAlarmAsync(client.Id, alarmData, cancellationToken);

                _logger.LogInformation("Successfully processed alarm {AlarmId} from client {ClientId}",
                    incomingAlarm.AlarmId, clientId);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Business logic error processing alarm from client {ClientId}: {Error}",
                    clientId, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing TCP message from client {ClientId}", clientId);
                return false;
            }
        }

        public async Task<IncomingAlarmDto?> ParseAlarmMessageAsync(string jsonMessage, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonMessage))
                {
                    _logger.LogWarning("Empty or null JSON message received");
                    return null;
                }

                // Parse JSON
                var incomingAlarm = JsonSerializer.Deserialize<IncomingAlarmDto>(jsonMessage, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (incomingAlarm == null)
                {
                    _logger.LogWarning("Failed to deserialize JSON message");
                    return null;
                }

                // Validate parsed data
                var validationResult = await _validator.ValidateAsync(incomingAlarm, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                    _logger.LogWarning("Invalid alarm message format: {Errors}", errors);
                    return null;
                }

                return incomingAlarm;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("JSON parsing error: {Error}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing alarm message");
                return null;
            }
        }

        public bool IsValidAlarmMessage(string jsonMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonMessage))
                    return false;

                var incomingAlarm = JsonSerializer.Deserialize<IncomingAlarmDto>(jsonMessage, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (incomingAlarm == null)
                    return false;

                var validationResult = _validator.Validate(incomingAlarm);
                return validationResult.IsValid;
            }
            catch
            {
                return false;
            }
        }
    }
}