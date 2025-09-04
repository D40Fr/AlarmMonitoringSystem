// AlarmMonitoringSystem.Web/Controllers/Api/DebugApiController.cs
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugApiController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ITcpServerService _tcpServerService;
        private readonly IConnectionLogService _connectionLogService;
        private readonly ILogger<DebugApiController> _logger;

        public DebugApiController(
            IClientService clientService,
            ITcpServerService tcpServerService,
            IConnectionLogService connectionLogService,
            ILogger<DebugApiController> logger)
        {
            _clientService = clientService;
            _tcpServerService = tcpServerService;
            _connectionLogService = connectionLogService;
            _logger = logger;
        }

        // GET: api/debug/connection-status
        [HttpGet("connection-status")]
        public async Task<ActionResult<ApiResponseDto<object>>> GetConnectionStatus()
        {
            try
            {
                // Get TCP server status
                var tcpConnectedClients = await _tcpServerService.GetConnectedClientIdsAsync();
                var tcpConnectedCount = await _tcpServerService.GetConnectedClientCountAsync();
                var serverStatus = await _tcpServerService.GetServerStatusAsync();

                // Get database status
                var allClients = await _clientService.GetAllClientsAsync();
                var connectedClientsInDb = await _clientService.GetClientsByStatusAsync(
                    Domain.Enums.ConnectionStatus.Connected);
                var dbConnectedCount = connectedClientsInDb.Count();

                // Get recent connection logs
                var recentLogs = await _connectionLogService.GetRecentLogsAsync(10);

                var debugInfo = new
                {
                    TcpServer = new
                    {
                        IsRunning = _tcpServerService.IsRunning,
                        Port = _tcpServerService.Port,
                        ConnectedClientIds = tcpConnectedClients.ToList(),
                        ConnectedCount = tcpConnectedCount,
                        ServerStatus = serverStatus
                    },
                    Database = new
                    {
                        TotalClients = allClients.Count(),
                        ConnectedCount = dbConnectedCount,
                        ConnectedClients = connectedClientsInDb.Select(c => new
                        {
                            c.Id,
                            c.ClientId,
                            c.Name,
                            c.Status,
                            c.LastConnectedAt,
                        }).ToList(),
                        AllClientsStatus = allClients.Select(c => new
                        {
                            c.Id,
                            c.ClientId,
                            c.Name,
                            c.Status,
                            c.LastConnectedAt,
                        }).ToList()
                    },
                    RecentConnectionLogs = recentLogs.Select(log => new
                    {
                        log.Id,
                        log.ClientId,
                        log.Status,
                        log.LogTime,
                        log.Message,
                    }).ToList(),
                    Summary = new
                    {
                        TcpConnectedCount = tcpConnectedCount,
                        DbConnectedCount = dbConnectedCount,
                        Discrepancy = tcpConnectedCount != dbConnectedCount,
                        ServerRunning = _tcpServerService.IsRunning
                    }
                };

                return Ok(ApiResponseDto<object>.SuccessResult(debugInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection status debug info");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }

        // POST: api/debug/cleanup-connections
        [HttpPost("cleanup-connections")]
        public async Task<ActionResult<ApiResponseDto<object>>> CleanupConnections()
        {
            try
            {
                var connectedClients = await _clientService.GetClientsByStatusAsync(
                    Domain.Enums.ConnectionStatus.Connected);

                var cleanupResults = new List<object>();

                foreach (var client in connectedClients)
                {
                    var lastLog = await _connectionLogService.GetLastConnectionLogAsync(client.Id);

                    if (lastLog != null &&
                        lastLog.Status == Domain.Enums.ConnectionStatus.Connected &&
                        DateTime.UtcNow - lastLog.LogTime > TimeSpan.FromMinutes(1)) // 1 minute for debugging
                    {
                        await _clientService.UpdateClientStatusAsync(client.Id,
                            Domain.Enums.ConnectionStatus.Disconnected);

                        await _connectionLogService.LogClientDisconnectedAsync(client.Id,
                            "Manual cleanup - client appears dead");

                        cleanupResults.Add(new
                        {
                            ClientId = client.ClientId,
                            ClientName = client.Name,
                            LastActivity = lastLog.LogTime,
                            MinutesInactive = (DateTime.UtcNow - lastLog.LogTime).TotalMinutes,
                            Action = "Disconnected"
                        });
                    }
                }

                return Ok(ApiResponseDto<object>.SuccessResult(new
                {
                    CleanupsPerformed = cleanupResults.Count,
                    Results = cleanupResults
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual connection cleanup");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }
    }
}