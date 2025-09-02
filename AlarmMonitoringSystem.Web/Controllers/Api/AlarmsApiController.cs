using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmsApiController : ControllerBase
    {
        private readonly IAlarmService _alarmService;
        private readonly IMapper _mapper;
        private readonly ILogger<AlarmsApiController> _logger;

        public AlarmsApiController(
            IAlarmService alarmService,
            IMapper mapper,
            ILogger<AlarmsApiController> logger)
        {
            _alarmService = alarmService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/alarms
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<List<AlarmDto>>>> GetAlarms([FromQuery] int count = 20)
        {
            try
            {
                var alarms = await _alarmService.GetRecentAlarmsAsync(count);
                var alarmDtos = _mapper.Map<List<AlarmDto>>(alarms);

                return Ok(ApiResponseDto<List<AlarmDto>>.SuccessResult(alarmDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarms via API");
                return StatusCode(500, ApiResponseDto<List<AlarmDto>>.ErrorResult("Internal server error"));
            }
        }

        // GET: api/alarms/unacknowledged
        [HttpGet("unacknowledged")]
        public async Task<ActionResult<ApiResponseDto<List<AlarmDto>>>> GetUnacknowledgedAlarms()
        {
            try
            {
                var alarms = await _alarmService.GetUnacknowledgedAlarmsAsync();
                var alarmDtos = _mapper.Map<List<AlarmDto>>(alarms);

                return Ok(ApiResponseDto<List<AlarmDto>>.SuccessResult(alarmDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unacknowledged alarms via API");
                return StatusCode(500, ApiResponseDto<List<AlarmDto>>.ErrorResult("Internal server error"));
            }
        }

        // POST: api/alarms/{id}/acknowledge
        [HttpPost("{id}/acknowledge")]
        public async Task<ActionResult<ApiResponseDto<object>>> AcknowledgeAlarm(Guid id, [FromBody] AcknowledgeRequest request)
        {
            try
            {
                var acknowledgedBy = request?.AcknowledgedBy ?? "API User";
                await _alarmService.AcknowledgeAlarmAsync(id, acknowledgedBy);

                return Ok(ApiResponseDto<object>.SuccessResult(null, "Alarm acknowledged successfully"));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponseDto<object>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alarm {AlarmId} via API", id);
                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }

        // GET: api/alarms/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponseDto<object>>> GetAlarmStatistics()
        {
            try
            {
                var totalCount = await _alarmService.GetAlarmCountAsync();
                var activeCount = await _alarmService.GetActiveAlarmCountAsync();
                var unacknowledgedCount = await _alarmService.GetUnacknowledgedAlarmCountAsync();
                var severityCounts = await _alarmService.GetAlarmCountsBySeverityAsync();
                var typeCounts = await _alarmService.GetAlarmCountsByTypeAsync();

                var statistics = new
                {
                    TotalAlarms = totalCount,
                    ActiveAlarms = activeCount,
                    UnacknowledgedAlarms = unacknowledgedCount,
                    AlarmsBySeverity = severityCounts,
                    AlarmsByType = typeCounts
                };

                return Ok(ApiResponseDto<object>.SuccessResult(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm statistics via API");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }
    }

    public class AcknowledgeRequest
    {
        public string AcknowledgedBy { get; set; } = string.Empty;
    }
}