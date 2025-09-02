// AlarmMonitoringSystem.Infrastructure/TcpServer/TcpServerService.cs
using AlarmMonitoringSystem.Application.Services;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Domain.ValueObjects;
using AlarmMonitoringSystem.Infrastructure.TcpServer.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace AlarmMonitoringSystem.Infrastructure.TcpServer
{
    public class TcpServerService : ITcpServerService, IDisposable
    {
        private readonly ITcpMessageProcessorService _messageProcessor;
        private readonly IClientService _clientService;
        private readonly IConnectionLogService _connectionLogService;
        private readonly ILogger<TcpServerService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly TcpServerConfiguration _configuration;

        private TcpListener? _tcpListener;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly ConcurrentDictionary<string, TcpClientHandler> _connectedClients = new();
        private Task? _serverTask;
        private DateTime? _startTime;

        // Statistics
        private long _totalConnections = 0;
        private long _totalMessagesReceived = 0;
        private long _totalMessagesProcessed = 0;

        public TcpServerService(
            ITcpMessageProcessorService messageProcessor,
            IClientService clientService,
            IConnectionLogService connectionLogService,
            ILogger<TcpServerService> logger,
            ILoggerFactory loggerFactory,
            TcpServerConfiguration configuration)
        {
            _messageProcessor = messageProcessor;
            _clientService = clientService;
            _connectionLogService = connectionLogService;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _configuration = configuration;
        }

        public bool IsRunning => _tcpListener?.Server?.IsBound == true;
        public int? Port => _tcpListener?.LocalEndpoint is IPEndPoint endpoint ? endpoint.Port : null;

        // Events from ITcpServerService interface
        public event Func<ClientInfo, Task>? ClientConnected;
        public event Func<string, string?, Task>? ClientDisconnected;
        public event Func<string, AlarmData, Task>? AlarmReceived;
        public event Func<string, string, Exception?, Task>? ErrorOccurred;

        public async Task StartAsync(int port, CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                _logger.LogWarning("TCP server is already running on port {Port}", Port);
                return;
            }

            try
            {
                _logger.LogInformation("Starting TCP server on port {Port}", port);

                _configuration.Port = port;
                _cancellationTokenSource = new CancellationTokenSource();
                _startTime = DateTime.UtcNow;

                // Create TCP listener
                _tcpListener = new TcpListener(IPAddress.Parse(_configuration.IpAddress), port);
                _tcpListener.Start();

                _logger.LogInformation("TCP server started successfully on {IpAddress}:{Port}",
                    _configuration.IpAddress, port);

                // Start accepting connections
                _serverTask = AcceptClientsAsync(_cancellationTokenSource.Token);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start TCP server on port {Port}", port);
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsRunning)
            {
                _logger.LogInformation("TCP server is not running");
                return;
            }

            try
            {
                _logger.LogInformation("Stopping TCP server...");

                // Cancel the server task
                _cancellationTokenSource?.Cancel();

                // Disconnect all clients
                await DisconnectAllClientsAsync(cancellationToken);

                // Stop the listener
                _tcpListener?.Stop();

                // Wait for server task to complete
                if (_serverTask != null)
                {
                    await _serverTask;
                }

                _logger.LogInformation("TCP server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TCP server");
                throw;
            }
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("TCP server is now accepting client connections...");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Accept incoming connection
                        var tcpClient = await _tcpListener!.AcceptTcpClientAsync();
                        Interlocked.Increment(ref _totalConnections);

                        // Handle the new client connection
                        _ = Task.Run(async () => await HandleNewClientAsync(tcpClient, cancellationToken), cancellationToken);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Server is shutting down
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error accepting TCP client connection");
                        await Task.Delay(1000, cancellationToken); // Brief delay before retrying
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TCP server accept loop cancelled");
            }
        }

        private async Task HandleNewClientAsync(TcpClient tcpClient, CancellationToken cancellationToken)
        {
            string? clientId = null;
            TcpClientHandler? clientHandler = null;

            try
            {
                // Get client endpoint information
                var remoteEndpoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                var ipAddress = remoteEndpoint?.Address.ToString() ?? "Unknown";
                var port = remoteEndpoint?.Port ?? 0;

                _logger.LogInformation("New TCP connection from {IpAddress}:{Port}", ipAddress, port);

                // Generate or determine client ID (for simplicity, use IP:Port or let client send ID)
                clientId = $"CLIENT_{ipAddress}_{port}_{DateTime.UtcNow.Ticks}";

                // Create client info
                var clientInfo = new TcpClientInfo
                {
                    ClientId = clientId,
                    TcpClient = tcpClient,
                    Stream = tcpClient.GetStream(),
                    IpAddress = ipAddress,
                    Port = port,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    Status = Domain.Enums.ConnectionStatus.Connected
                };

                // Create logger specifically for TcpClientHandler
                var clientHandlerLogger = _loggerFactory.CreateLogger<TcpClientHandler>();

                // Create client handler
                clientHandler = new TcpClientHandler(
                    clientInfo,
                    _messageProcessor,
                    _connectionLogService,
                    clientHandlerLogger,
                    _configuration);

                // Subscribe to client events
                clientHandler.ClientDisconnected += OnClientDisconnected;
                clientHandler.MessageReceived += OnMessageReceived;
                clientHandler.ErrorOccurred += OnClientError;

                // Add to connected clients
                _connectedClients.TryAdd(clientId, clientHandler);

                // Register client in business layer
                await RegisterClientInBusinessLayer(clientInfo, cancellationToken);

                // Log connection
                await _connectionLogService.LogClientConnectedAsync(
                    Guid.Parse(clientId), // This might need adjustment based on your client ID strategy
                    ipAddress,
                    port,
                    cancellationToken);

                // Notify connection
                await NotifyClientConnected(clientInfo);

                // Start handling this client
                await clientHandler.StartHandlingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling new client connection from {ClientId}", clientId ?? "Unknown");

                if (clientId != null)
                {
                    _connectedClients.TryRemove(clientId, out _);
                }

                clientHandler?.Dispose();
                tcpClient.Close();
            }
        }

        private async Task RegisterClientInBusinessLayer(TcpClientInfo clientInfo, CancellationToken cancellationToken)
        {
            try
            {
                // Check if client already exists
                var existingClient = await _clientService.GetClientByClientIdAsync(clientInfo.ClientId, cancellationToken);

                if (existingClient == null)
                {
                    // Register new client
                    var clientInfoVO = ClientInfo.Create(
                        clientInfo.ClientId,
                        $"TCP Client {clientInfo.ClientId}",
                        clientInfo.IpAddress,
                        clientInfo.Port,
                        $"Auto-registered TCP client from {clientInfo.IpAddress}:{clientInfo.Port}");

                    await _clientService.RegisterClientAsync(clientInfoVO, cancellationToken);
                    _logger.LogInformation("Auto-registered new client {ClientId}", clientInfo.ClientId);
                }
                else
                {
                    // Update existing client status
                    await _clientService.UpdateClientStatusAsync(clientInfo.ClientId, Domain.Enums.ConnectionStatus.Connected, cancellationToken);
                    _logger.LogInformation("Updated existing client {ClientId} status to Connected", clientInfo.ClientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering client {ClientId} in business layer", clientInfo.ClientId);
                // Don't throw - allow TCP connection to continue even if business registration fails
            }
        }

        public async Task<bool> DisconnectClientAsync(string clientId, CancellationToken cancellationToken = default)
        {
            if (_connectedClients.TryGetValue(clientId, out var clientHandler))
            {
                await clientHandler.DisconnectAsync("Server requested disconnection");
                return true;
            }
            return false;
        }

        public async Task<bool> DisconnectClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            // Find client by Guid (using the public ClientInfo property)
            var clientHandler = _connectedClients.Values.FirstOrDefault(c => c.ClientInfo.ClientId == clientId.ToString());
            if (clientHandler != null)
            {
                await clientHandler.DisconnectAsync("Server requested disconnection");
                return true;
            }
            return false;
        }

        public async Task DisconnectAllClientsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Disconnecting all clients...");

            var disconnectTasks = _connectedClients.Values.Select(client =>
                client.DisconnectAsync("Server shutdown"));

            await Task.WhenAll(disconnectTasks);
            _connectedClients.Clear();

            _logger.LogInformation("All clients disconnected");
        }

        public async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(message))
                return;

            _logger.LogInformation("Broadcasting message to {Count} clients", _connectedClients.Count);

            var tasks = _connectedClients.Values.Select(client => client.SendMessageAsync(message));
            await Task.WhenAll(tasks);
        }

        public async Task SendMessageToClientAsync(string clientId, string message, CancellationToken cancellationToken = default)
        {
            if (_connectedClients.TryGetValue(clientId, out var clientHandler))
            {
                await clientHandler.SendMessageAsync(message);
            }
            else
            {
                _logger.LogWarning("Client {ClientId} not found for message sending", clientId);
            }
        }

        public async Task SendMessageToClientAsync(Guid clientId, string message, CancellationToken cancellationToken = default)
        {
            var clientHandler = _connectedClients.Values.FirstOrDefault(c => c.ClientInfo.ClientId == clientId.ToString());
            if (clientHandler != null)
            {
                await clientHandler.SendMessageAsync(message);
            }
            else
            {
                _logger.LogWarning("Client {ClientId} not found for message sending", clientId);
            }
        }

        public async Task<IEnumerable<string>> GetConnectedClientIdsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_connectedClients.Keys.ToList());
        }

        public async Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_connectedClients.Count);
        }

        public async Task<Dictionary<string, object>> GetServerStatusAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(new Dictionary<string, object>
            {
                { "IsRunning", IsRunning },
                { "Port", Port ?? 0 },
                { "StartTime", _startTime ?? DateTime.MinValue },
                { "Uptime", _startTime.HasValue ? DateTime.UtcNow - _startTime.Value : TimeSpan.Zero },
                { "ConnectedClients", _connectedClients.Count },
                { "TotalConnections", _totalConnections },
                { "TotalMessagesReceived", _totalMessagesReceived },
                { "TotalMessagesProcessed", _totalMessagesProcessed },
                { "MaxConnections", _configuration.MaxConnections },
                { "ServerConfiguration", _configuration }
            });
        }

        public async Task<TimeSpan> GetUptimeAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_startTime.HasValue ? DateTime.UtcNow - _startTime.Value : TimeSpan.Zero);
        }

        public async Task<long> GetTotalMessagesReceivedAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_totalMessagesReceived);
        }

        public async Task<long> GetTotalMessagesProcessedAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_totalMessagesProcessed);
        }

        // Event handlers for client events
        private async Task OnClientDisconnected(string clientId)
        {
            _logger.LogInformation("Client {ClientId} disconnected", clientId);

            // Remove from connected clients
            _connectedClients.TryRemove(clientId, out _);

            // Update client status in business layer
            try
            {
                await _clientService.UpdateClientStatusAsync(clientId, Domain.Enums.ConnectionStatus.Disconnected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client {ClientId} status to disconnected", clientId);
            }

            // Notify external listeners
            try
            {
                if (ClientDisconnected != null)
                    await ClientDisconnected.Invoke(clientId, "Client disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying client {ClientId} disconnection", clientId);
            }
        }

        private async Task OnMessageReceived(TcpMessage message)
        {
            Interlocked.Increment(ref _totalMessagesReceived);
            _logger.LogDebug("Message received from client {ClientId}, Type: {MessageType}",
                message.ClientId, message.MessageType);

            try
            {
                // Message is already processed by TcpClientHandler
                // This is just for statistics and additional notifications
                Interlocked.Increment(ref _totalMessagesProcessed);

                // If this was an alarm message, we could notify external listeners
                // For now, we'll keep it simple
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message received event from client {ClientId}", message.ClientId);
            }
        }

        private async Task OnClientError(string clientId, Exception exception)
        {
            _logger.LogError(exception, "Error occurred for client {ClientId}", clientId);

            try
            {
                // Log error in business layer
                if (Guid.TryParse(clientId, out var clientGuid))
                {
                    await _connectionLogService.LogConnectionErrorAsync(clientGuid, exception.Message, exception.StackTrace);
                }

                // Notify external listeners
                if (ErrorOccurred != null)
                    await ErrorOccurred.Invoke(clientId, exception.Message, exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client error event for {ClientId}", clientId);
            }
        }

        private async Task NotifyClientConnected(TcpClientInfo clientInfo)
        {
            try
            {
                var clientInfoVO = ClientInfo.Create(
                    clientInfo.ClientId,
                    $"TCP Client {clientInfo.ClientId}",
                    clientInfo.IpAddress,
                    clientInfo.Port,
                    $"Connected from {clientInfo.IpAddress}:{clientInfo.Port}");

                if (ClientConnected != null)
                    await ClientConnected.Invoke(clientInfoVO);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying client {ClientId} connection", clientInfo.ClientId);
            }
        }

        // Cleanup and monitoring
        public async Task PerformMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Performing TCP server maintenance...");

            var disconnectedClients = new List<string>();

            foreach (var kvp in _connectedClients)
            {
                var clientId = kvp.Key;
                var clientHandler = kvp.Value;

                if (!clientHandler.IsClientAlive())
                {
                    _logger.LogWarning("Client {ClientId} appears to be dead, disconnecting...", clientId);
                    disconnectedClients.Add(clientId);
                }
            }

            // Disconnect dead clients
            foreach (var clientId in disconnectedClients)
            {
                if (_connectedClients.TryGetValue(clientId, out var clientHandler))
                {
                    await clientHandler.DisconnectAsync("Connection timeout");
                }
            }

            if (disconnectedClients.Any())
            {
                _logger.LogInformation("Disconnected {Count} inactive clients during maintenance", disconnectedClients.Count);
            }
        }

        public void Dispose()
        {
            try
            {
                StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TCP server disposal");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _tcpListener?.Stop();
            }
        }
    }
}