using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers
{
    public class AlarmsController : Controller
    {
        private readonly IAlarmService _alarmService;
        private readonly IClientService _clientService;
        private readonly IMapper _mapper;
        private readonly ILogger<AlarmsController> _logger;

        public AlarmsController(
            IAlarmService alarmService,
            IClientService clientService,
            IMapper mapper,
            ILogger<AlarmsController> logger)
        {
            _alarmService = alarmService;
            _clientService = clientService;
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
                await _alarmService.AcknowledgeAlarmAsync(id, userName);

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
    }
}
