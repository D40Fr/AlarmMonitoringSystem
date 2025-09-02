using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Domain.ValueObjects;
using AutoMapper;
using Microsoft.Extensions.Logging;
using DomainLogLevel = AlarmMonitoringSystem.Domain.Enums.LogLevel; // Fix namespace conflict
using DomainConnectionStatus = AlarmMonitoringSystem.Domain.Enums.ConnectionStatus;

namespace AlarmMonitoringSystem.Application.Services
{
    public class ConnectionLogService : IConnectionLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ConnectionLogService> _logger;

        public ConnectionLogService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ConnectionLogService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task LogConnectionEventAsync(ConnectionEvent connectionEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Logging connection event for client {ClientId}: {Status}",
                connectionEvent.ClientId, connectionEvent.Status);

            var connectionLog = new ConnectionLog
            {
                ClientId = connectionEvent.ClientId,
                Status = connectionEvent.Status,
                Message = connectionEvent.Message,
                LogLevel = connectionEvent.LogLevel,
                LogTime = connectionEvent.EventTime,
                IpAddress = connectionEvent.IpAddress,
                Port = connectionEvent.Port,
                Details = connectionEvent.Details
            };

            await _unitOfWork.ConnectionLogs.AddAsync(connectionLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task LogClientConnectedAsync(Guid clientId, string? ipAddress = null, int? port = null, CancellationToken cancellationToken = default)
        {
            var connectionEvent = ConnectionEvent.Create(
                clientId,
                DomainConnectionStatus.Connected,
                "Client connected",
                DomainLogLevel.Information,
                ipAddress,
                port);

            await LogConnectionEventAsync(connectionEvent, cancellationToken);
            _logger.LogInformation("Client {ClientId} connected from {IpAddress}:{Port}", clientId, ipAddress, port);
        }

        public async Task LogClientDisconnectedAsync(Guid clientId, string? reason = null, CancellationToken cancellationToken = default)
        {
            var message = string.IsNullOrEmpty(reason) ? "Client disconnected" : $"Client disconnected: {reason}";

            var connectionEvent = ConnectionEvent.Create(
                clientId,
                DomainConnectionStatus.Disconnected,
                message,
                DomainLogLevel.Information);

            await LogConnectionEventAsync(connectionEvent, cancellationToken);
            _logger.LogInformation("Client {ClientId} disconnected. Reason: {Reason}", clientId, reason ?? "Unknown");
        }

        public async Task LogConnectionErrorAsync(Guid clientId, string errorMessage, string? details = null, CancellationToken cancellationToken = default)
        {
            var connectionEvent = ConnectionEvent.Create(
                clientId,
                DomainConnectionStatus.Error,
                errorMessage,
                DomainLogLevel.Error,
                details: details);

            await LogConnectionEventAsync(connectionEvent, cancellationToken);
            _logger.LogError("Connection error for client {ClientId}: {Error}", clientId, errorMessage);
        }

        public async Task<IEnumerable<ConnectionLog>> GetConnectionLogsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetRecentLogsAsync(100, cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetClientConnectionLogsAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetByClientIdAsync(clientId, cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetLogsByStatusAsync(DomainConnectionStatus status, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetByStatusAsync(status, cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetLogsByLevelAsync(DomainLogLevel logLevel, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetByLogLevelAsync(logLevel, cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetRecentLogsAsync(count, cancellationToken);
        }

        public async Task<IEnumerable<ConnectionLog>> GetClientConnectionHistoryAsync(Guid clientId, int count, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetClientConnectionHistoryAsync(clientId, count, cancellationToken);
        }

        public async Task<ConnectionLog?> GetLastConnectionLogAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.GetLastConnectionLogAsync(clientId, cancellationToken);
        }

        public async Task CleanupOldLogsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            await _unitOfWork.ConnectionLogs.CleanupOldLogsAsync(cutoffDate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up connection logs older than {CutoffDate}", cutoffDate);
        }

        public async Task<int> GetLogCountAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.ConnectionLogs.CountAsync(cancellationToken);
        }

        public async Task<Dictionary<DomainLogLevel, int>> GetLogCountsByLevelAsync(CancellationToken cancellationToken = default)
        {
            // Use basic repository methods to get counts
            var allLogs = await _unitOfWork.ConnectionLogs.GetAllAsync(cancellationToken);
            return allLogs
                .GroupBy(log => log.LogLevel)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}