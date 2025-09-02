using AlarmMonitoringSystem.Web.Models;
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AlarmMonitoringSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IClientService _clientService;
        private readonly IAlarmService _alarmService;
        private readonly IConnectionLogService _connectionLogService;
        private readonly ITcpServerService _tcpServerService;
        private readonly IMapper _mapper;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IClientService clientService,
            IAlarmService alarmService,
            IConnectionLogService connectionLogService,
            ITcpServerService tcpServerService,
            IMapper mapper,
            ILogger<HomeController> logger)
        {
            _clientService = clientService;
            _alarmService = alarmService;
            _connectionLogService = connectionLogService;
            _tcpServerService = tcpServerService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get dashboard data
                var clients = await _clientService.GetAllClientsAsync();
                var recentAlarms = await _alarmService.GetRecentAlarmsAsync(10);
                var recentLogs = await _connectionLogService.GetRecentLogsAsync(10);
                var serverStatus = await _tcpServerService.GetServerStatusAsync();

                // Map to DTOs
                var clientDtos = _mapper.Map<List<ClientDto>>(clients);
                var alarmDtos = _mapper.Map<List<AlarmDto>>(recentAlarms);
                var logDtos = _mapper.Map<List<ConnectionLogDto>>(recentLogs);

                // Create dashboard view model
                var viewModel = new DashboardViewModel
                {
                    Clients = clientDtos,
                    RecentAlarms = alarmDtos,
                    RecentConnectionLogs = logDtos,
                    Statistics = new DashboardStatistics
                    {
                        TotalClients = clientDtos.Count,
                        ConnectedClients = clientDtos.Count(c => c.Status == Domain.Enums.ConnectionStatus.Connected),
                        TotalAlarms = await _alarmService.GetAlarmCountAsync(),
                        ActiveAlarms = await _alarmService.GetActiveAlarmCountAsync(),
                        UnacknowledgedAlarms = await _alarmService.GetUnacknowledgedAlarmCountAsync(),
                        TcpServerPort = serverStatus.ContainsKey("Port") ? (int)serverStatus["Port"] : 0,
                        TcpServerUptime = serverStatus.ContainsKey("Uptime") ? (TimeSpan)serverStatus["Uptime"] : TimeSpan.Zero,
                        TotalMessagesReceived = serverStatus.ContainsKey("TotalMessagesReceived") ? (long)serverStatus["TotalMessagesReceived"] : 0
                    }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}