using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConnectionLogsApiController : ControllerBase
    {
        private readonly IConnectionLogService _connectionLogService;
        private readonly IMapper _mapper;
        private readonly ILogger<ConnectionLogsApiController> _logger;

        public ConnectionLogsApiController(
            IConnectionLogService connectionLogService,
            IMapper mapper,
            ILogger<ConnectionLogsApiController> logger)
        {
            _connectionLogService = connectionLogService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/connectionlogs/recent
        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponseDto<List<ConnectionLogDto>>>> GetRecentLogs([FromQuery] int count = 10)
        {
            try
            {
                var logs = await _connectionLogService.GetRecentLogsAsync(count);
                var logDtos = _mapper.Map<List<ConnectionLogDto>>(logs);

                return Ok(ApiResponseDto<List<ConnectionLogDto>>.SuccessResult(logDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent connection logs via API");
                return StatusCode(500, ApiResponseDto<List<ConnectionLogDto>>.ErrorResult("Internal server error"));
            }
        }

        // Add other endpoints as needed...
    }
}