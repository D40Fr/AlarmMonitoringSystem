using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Web.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers
{
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;
        private readonly IAlarmService _alarmService;
        private readonly IConnectionLogService _connectionLogService;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(
            IClientService clientService,
            IAlarmService alarmService,
            IConnectionLogService connectionLogService,
            IMapper mapper,
            ILogger<ClientsController> logger)
        {
            _clientService = clientService;
            _alarmService = alarmService;
            _connectionLogService = connectionLogService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                var clientDtos = _mapper.Map<List<ClientDto>>(clients);

                // Add alarm counts for each client
                foreach (var clientDto in clientDtos)
                {
                    var clientAlarms = await _alarmService.GetClientAlarmsAsync(clientDto.Id);
                    clientDto.ActiveAlarmCount = clientAlarms.Count(a => a.IsActive && !a.IsAcknowledged);
                    clientDto.LastAlarmTime = clientAlarms.Where(a => a.IsActive).Max(a => a.AlarmTime as DateTime?);
                }

                return View(clientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clients");
                TempData["Error"] = "Failed to load clients";
                return View(new List<ClientDto>());
            }
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var client = await _clientService.GetClientAsync(id);
                if (client == null)
                {
                    return NotFound();
                }

                var clientDto = _mapper.Map<ClientDto>(client);

                // Get client's alarms and connection logs
                var alarms = await _alarmService.GetClientAlarmsAsync(id);
                var connectionLogs = await _connectionLogService.GetClientConnectionLogsAsync(id);

                var viewModel = new ClientDetailsViewModel
                {
                    Client = clientDto,
                    Alarms = _mapper.Map<List<AlarmDto>>(alarms.Take(20)), // Last 20 alarms
                    ConnectionLogs = _mapper.Map<List<ConnectionLogDto>>(connectionLogs.Take(50)) // Last 50 logs
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client details for {ClientId}", id);
                return NotFound();
            }
        }
    }
}