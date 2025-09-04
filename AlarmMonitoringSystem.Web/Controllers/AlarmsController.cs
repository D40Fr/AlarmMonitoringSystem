// AlarmMonitoringSystem.Web/Controllers/AlarmsController.cs
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Application.Interfaces;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Web.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers
{
    public class AlarmsController : Controller
    {
        private readonly IAlarmService _alarmService;
        private readonly IClientService _clientService;
        private readonly IRealtimeNotificationService _realtimeNotificationService; // ✅ ADD: SignalR
        private readonly IMapper _mapper;
        private readonly ILogger<AlarmsController> _logger;

        public AlarmsController(
            IAlarmService alarmService,
            IClientService clientService,
            IRealtimeNotificationService signalRNotificationService, // ✅ ADD: SignalR
            IMapper mapper,
            ILogger<AlarmsController> logger)
        {
            _alarmService = alarmService;
            _clientService = clientService;
            _realtimeNotificationService = signalRNotificationService; // ✅ ADD: SignalR
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Alarms
        public async Task<IActionResult> Index(string? severity, string? type, bool? acknowledged)
        {
            try
            {
                var alarms = await _alarmService.GetActiveAlarmsAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(severity) && Enum.TryParse<Domain.Enums.AlarmSeverity>(severity, out var severityFilter))
                {
                    alarms = alarms.Where(a => a.Severity == severityFilter);
                }

                if (!string.IsNullOrEmpty(type) && Enum.TryParse<Domain.Enums.AlarmType>(type, out var typeFilter))
                {
                    alarms = alarms.Where(a => a.Type == typeFilter);
                }

                if (acknowledged.HasValue)
                {
                    alarms = alarms.Where(a => a.IsAcknowledged == acknowledged.Value);
                }

                var alarmDtos = _mapper.Map<List<AlarmDto>>(alarms.OrderByDescending(a => a.AlarmTime));

                ViewBag.Severity = severity;
                ViewBag.Type = type;
                ViewBag.Acknowledged = acknowledged;

                return View(alarmDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading alarms");
                TempData["Error"] = "Failed to load alarms";
                return View(new List<AlarmDto>());
            }
        }

        // GET: Alarms/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var alarm = await _alarmService.GetAlarmAsync(id);
                if (alarm == null)
                {
                    return NotFound();
                }

                var alarmDto = _mapper.Map<AlarmDto>(alarm);
                return View(alarmDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading alarm details for {AlarmId}", id);
                return NotFound();
            }
        }

        // POST: Alarms/Acknowledge/5
        [HttpPost]
        public async Task<IActionResult> Acknowledge(Guid id)
        {
            try
            {
                var userName = "Web User"; // You can get this from authentication later
                var acknowledgedAlarm = await _alarmService.AcknowledgeAlarmAsync(id, userName);

                // ✅ ADD: Broadcast acknowledgment (this is handled in AlarmService now, but we can add UI refresh)
                try
                {
                    await _realtimeNotificationService.RefreshDashboardStatsAsync();
                    await _realtimeNotificationService.RefreshAlarmListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast UI refresh for alarm acknowledgment {AlarmId}", id);
                    // Don't fail the operation
                }

                TempData["Success"] = "Alarm acknowledged successfully";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alarm {AlarmId}", id);
                TempData["Error"] = "Failed to acknowledge alarm";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ✅ ADD: AJAX endpoint for real-time alarm updates
        [HttpGet]
        public async Task<IActionResult> GetRecentAlarms(int count = 10)
        {
            try
            {
                var alarms = await _alarmService.GetRecentAlarmsAsync(count);
                var alarmDtos = _mapper.Map<List<AlarmDto>>(alarms);

                return Json(new { success = true, alarms = alarmDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent alarms");
                return Json(new { success = false, error = "Failed to load recent alarms" });
            }
        }

        // ✅ ADD: AJAX endpoint for alarm statistics
        [HttpGet]
        public async Task<IActionResult> GetAlarmStats()
        {
            try
            {
                var totalCount = await _alarmService.GetAlarmCountAsync();
                var activeCount = await _alarmService.GetActiveAlarmCountAsync();
                var unacknowledgedCount = await _alarmService.GetUnacknowledgedAlarmCountAsync();

                return Json(new
                {
                    success = true,
                    stats = new
                    {
                        total = totalCount,
                        active = activeCount,
                        unacknowledged = unacknowledgedCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alarm statistics");
                return Json(new { success = false, error = "Failed to load alarm statistics" });
            }
        }
    }
}