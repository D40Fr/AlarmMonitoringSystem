using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlarmMonitoringSystem.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsApiController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ITcpServerService _tcpServerService;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientsApiController> _logger;

        public ClientsApiController(
            IClientService clientService,
            ITcpServerService tcpServerService,
            IMapper mapper,
            ILogger<ClientsApiController> logger)
        {
            _clientService = clientService;
            _tcpServerService = tcpServerService;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/clients
        [HttpGet]
        public async Task<ActionResult<ApiResponseDto<List<ClientDto>>>> GetClients()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                var clientDtos = _mapper.Map<List<ClientDto>>(clients);

                return Ok(ApiResponseDto<List<ClientDto>>.SuccessResult(clientDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clients via API");
                return StatusCode(500, ApiResponseDto<List<ClientDto>>.ErrorResult("Internal server error"));
            }
        }

        // GET: api/clients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponseDto<ClientDto>>> GetClient(Guid id)
        {
            try
            {
                var client = await _clientService.GetClientAsync(id);
                if (client == null)
                {
                    return NotFound(ApiResponseDto<ClientDto>.ErrorResult("Client not found"));
                }

                var clientDto = _mapper.Map<ClientDto>(client);
                return Ok(ApiResponseDto<ClientDto>.SuccessResult(clientDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client {ClientId} via API", id);
                return StatusCode(500, ApiResponseDto<ClientDto>.ErrorResult("Internal server error"));
            }
        }

        // GET: api/clients/status
        [HttpGet("status")]
        public async Task<ActionResult<ApiResponseDto<object>>> GetClientsStatus()
        {
            try
            {
                var statusCounts = await _clientService.GetClientStatusCountsAsync();
                var connectedCount = await _tcpServerService.GetConnectedClientCountAsync();

                var status = new
                {
                    StatusCounts = statusCounts,
                    ConnectedToTcpServer = connectedCount,
                    TotalRegistered = await _clientService.GetTotalClientCountAsync()
                };

                return Ok(ApiResponseDto<object>.SuccessResult(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client status via API");
                return StatusCode(500, ApiResponseDto<object>.ErrorResult("Internal server error"));
            }
        }
    }
}