// AlarmMonitoringSystem.Infrastructure/TcpServer/TcpServerService.cs
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Application.Interfaces;
using AlarmMonitoringSystem.Application.Services;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Domain.ValueObjects;
using AlarmMonitoringSystem.Infrastructure.TcpServer.Models;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace AlarmMonitoringSystem.Infrastructure.TcpServer
{
    public class TcpServerService : ITcpServerService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
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
            IServiceScopeFactory serviceScopeFactory,
            ILogger<TcpServerService> logger,
            ILoggerFactory loggerFactory,
            TcpServerConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
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
            Client? registeredClient = null;

            try
            {
                // Get client endpoint information
                var remoteEndpoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
                var ipAddress = remoteEndpoint?.Address.ToString() ?? "Unknown";
                var port = remoteEndpoint?.Port ?? 0;

                _logger.LogInformation("New TCP connection from {IpAddress}:{Port}", ipAddress, port);

                // Generate client ID
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

                // Create scope for this client's lifetime
                using var scope = _serviceScopeFactory.CreateScope();
                var messageProcessor = scope.ServiceProvider.GetRequiredService<ITcpMessageProcessorService>();
                var connectionLogService = scope.ServiceProvider.GetRequiredService<IConnectionLogService>();
                var clientHandlerLogger = _loggerFactory.CreateLogger<TcpClientHandler>();

                // Register client in business layer first
                registeredClient = await RegisterClientInBusinessLayer(clientInfo, scope.ServiceProvider, cancellationToken);

                // Create client handler with scope factory for disconnection handling
                clientHandler = new TcpClientHandler(
                    clientInfo,
                    messageProcessor,
                    connectionLogService,
                    _serviceScopeFactory,
                    clientHandlerLogger,
                    _configuration);

                // Subscribe to client events
                clientHandler.ClientDisconnected += OnClientDisconnected;
                clientHandler.MessageReceived += OnMessageReceived;
                clientHandler.ErrorOccurred += OnClientError;

                // Add to connected clients
                _connectedClients.TryAdd(clientId, clientHandler);

                // Log connection using the database client ID
                if (registeredClient != null)
                {
                    await connectionLogService.LogClientConnectedAsync(
                        registeredClient.Id,
                        ipAddress,
                        port,
                        cancellationToken);
                }

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

        private async Task<Client?> RegisterClientInBusinessLayer(
            TcpClientInfo clientInfo,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            try
            {
                var clientService = serviceProvider.GetRequiredService<IClientService>();

                // Check if client already exists
                var existingClient = await clientService.GetClientByClientIdAsync(clientInfo.ClientId, cancellationToken);

                if (existingClient == null)
                {
                    // Register new client
                    var clientInfoVO = ClientInfo.Create(
                        clientInfo.ClientId,
                        $"TCP Client {clientInfo.ClientId}",
                        clientInfo.IpAddress,
                        clientInfo.Port,
                        $"Auto-registered TCP client from {clientInfo.IpAddress}:{clientInfo.Port}");

                    var newClient = await clientService.RegisterClientAsync(clientInfoVO, cancellationToken);
                    _logger.LogInformation("Auto-registered new client {ClientId}", clientInfo.ClientId);
                    return newClient;
                }
                else
                {
                    // Update existing client status
                    await clientService.UpdateClientStatusAsync(clientInfo.ClientId, Domain.Enums.ConnectionStatus.Connected, cancellationToken);
                    _logger.LogInformation("Updated existing client {ClientId} status to Connected", clientInfo.ClientId);
                    return existingClient;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering client {ClientId} in business layer", clientInfo.ClientId);
                return null;
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

        // Return actual connected count from in-memory collection
        public async Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default)
        {
            // Remove dead connections from the collection
            await CleanupDeadConnections();
            return await Task.FromResult(_connectedClients.Count);
        }

        // Method to clean up dead connections
        private async Task CleanupDeadConnections()
        {
            var deadClients = new List<string>();

            foreach (var kvp in _connectedClients)
            {
                var clientHandler = kvp.Value;
                if (!clientHandler.IsClientAlive() || !clientHandler.ClientInfo.IsConnected)
                {
                    deadClients.Add(kvp.Key);
                }
            }

            foreach (var deadClientId in deadClients)
            {
                if (_connectedClients.TryRemove(deadClientId, out var deadClientHandler))
                {
                    _logger.LogInformation("Removing dead client {ClientId} from connected list", deadClientId);
                    await deadClientHandler.DisconnectAsync("Connection lost/dead");
                }
            }
        }

        public async Task<Dictionary<string, object>> GetServerStatusAsync(CancellationToken cancellationToken = default)
        {
            // Clean up dead connections before reporting status
            await CleanupDeadConnections();

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

        // Event handlers
        private async Task OnClientDisconnected(string clientId)
        {
            _logger.LogInformation("Client {ClientId} disconnected", clientId);

            // Remove from connected clients
            _connectedClients.TryRemove(clientId, out _);

            // Update client status in business layer using a new scope
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
                await clientService.UpdateClientStatusAsync(clientId, Domain.Enums.ConnectionStatus.Disconnected);

                _logger.LogInformation("Updated client {ClientId} status to Disconnected in database", clientId);

                // ? ADD: Broadcast client disconnection via SignalR
                try
                {
                    var signalRService = scope.ServiceProvider.GetService<IRealtimeNotificationService>();
                    if (signalRService != null)
                    {
                        await signalRService.NotifyClientDisconnectedAsync(clientId);
                        _logger.LogInformation("Successfully broadcasted client disconnection {ClientId} via SignalR", clientId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast client disconnection {ClientId} via SignalR", clientId);
                    // Don't fail the disconnection process if SignalR broadcast fails
                }
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
                Interlocked.Increment(ref _totalMessagesProcessed);
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
                // Log error in business layer using a new scope
                using var scope = _serviceScopeFactory.CreateScope();
                var connectionLogService = scope.ServiceProvider.GetRequiredService<IConnectionLogService>();

                if (Guid.TryParse(clientId, out var clientGuid))
                {
                    await connectionLogService.LogConnectionErrorAsync(clientGuid, exception.Message, exception.StackTrace);
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

                // ? ADD: Broadcast client connection via SignalR
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var signalRService = scope.ServiceProvider.GetService<IRealtimeNotificationService>();

                    if (signalRService != null)
                    {
                        // Find the registered client to get proper DTO
                        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
                        var registeredClient = await clientService.GetClientByClientIdAsync(clientInfo.ClientId);

                        if (registeredClient != null)
                        {
                            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                            var clientDto = mapper.Map<ClientDto>(registeredClient);

                            await signalRService.NotifyClientConnectedAsync(clientDto);
                            _logger.LogInformation("Successfully broadcasted client connection {ClientId} via SignalR", clientInfo.ClientId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to broadcast client connection {ClientId} via SignalR", clientInfo.ClientId);
                    // Don't fail the connection process if SignalR broadcast fails
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying client {ClientId} connection", clientInfo.ClientId);
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