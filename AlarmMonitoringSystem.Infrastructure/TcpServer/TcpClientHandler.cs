// AlarmMonitoringSystem.Infrastructure/TcpServer/TcpClientHandler.cs
using AlarmMonitoringSystem.Application.Services;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Infrastructure.TcpServer.Models;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace AlarmMonitoringSystem.Infrastructure.TcpServer
{
    public class TcpClientHandler : IDisposable
    {
        private readonly TcpClientInfo _clientInfo;
        private readonly ITcpMessageProcessorService _messageProcessor;
        private readonly IConnectionLogService _connectionLogService;
        private readonly ILogger<TcpClientHandler> _logger;
        private readonly TcpServerConfiguration _configuration;

        // Events
        public event Func<string, Task>? ClientDisconnected;
        public event Func<TcpMessage, Task>? MessageReceived;
        public event Func<string, Exception, Task>? ErrorOccurred;

        // Public property to access client info
        public TcpClientInfo ClientInfo => _clientInfo;

        public TcpClientHandler(
            TcpClientInfo clientInfo,
            ITcpMessageProcessorService messageProcessor,
            IConnectionLogService connectionLogService,
            ILogger<TcpClientHandler> logger,
            TcpServerConfiguration configuration)
        {
            _clientInfo = clientInfo;
            _messageProcessor = messageProcessor;
            _connectionLogService = connectionLogService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task StartHandlingAsync()
        {
            _logger.LogInformation("Starting TCP client handler for {ClientId} from {IpAddress}:{Port}",
                _clientInfo.ClientId, _clientInfo.IpAddress, _clientInfo.Port);

            try
            {
                // Start listening for messages
                await ListenForMessagesAsync(_clientInfo.CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TCP client handler for {ClientId} was cancelled", _clientInfo.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TCP client handler for {ClientId}", _clientInfo.ClientId);
                await NotifyErrorAsync(ex);
            }
            finally
            {
                await DisconnectAsync("Handler stopped");
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[_configuration.BufferSize];
            var messageBuilder = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested && _clientInfo.IsConnected)
            {
                try
                {
                    // Check if data is available
                    if (!_clientInfo.Stream.DataAvailable)
                    {
                        await Task.Delay(10, cancellationToken); // Small delay to prevent busy waiting
                        continue;
                    }

                    // Read data from stream
                    int bytesRead = await _clientInfo.Stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        _logger.LogInformation("Client {ClientId} disconnected (no data received)", _clientInfo.ClientId);
                        break;
                    }

                    // Convert bytes to string
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(receivedData);

                    // Check for complete messages (assuming messages end with newline)
                    await ProcessCompleteMessages(messageBuilder, cancellationToken);

                    _clientInfo.UpdateActivity();
                    _clientInfo.MessagesReceived++;
                }
                catch (IOException ex)
                {
                    _logger.LogWarning("IO error reading from client {ClientId}: {Error}", _clientInfo.ClientId, ex.Message);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogInformation("Stream disposed for client {ClientId}", _clientInfo.ClientId);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error reading from client {ClientId}", _clientInfo.ClientId);
                    await NotifyErrorAsync(ex);
                    break;
                }
            }
        }

        private async Task ProcessCompleteMessages(StringBuilder messageBuilder, CancellationToken cancellationToken)
        {
            string content = messageBuilder.ToString();
            string[] messages = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            // Process all complete messages except the last one (might be incomplete)
            for (int i = 0; i < messages.Length - 1; i++)
            {
                await ProcessSingleMessage(messages[i].Trim(), cancellationToken);
            }

            // Check if the last message is complete (ends with newline)
            if (content.EndsWith('\n') && messages.Length > 0)
            {
                await ProcessSingleMessage(messages[^1].Trim(), cancellationToken);
                messageBuilder.Clear();
            }
            else if (messages.Length > 0)
            {
                // Keep the incomplete message in buffer
                messageBuilder.Clear();
                messageBuilder.Append(messages[^1]);
            }
        }

        private async Task ProcessSingleMessage(string messageContent, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(messageContent))
                return;

            try
            {
                _logger.LogDebug("Processing message from client {ClientId}: {MessageLength} characters",
                    _clientInfo.ClientId, messageContent.Length);

                // Check message size limit
                if (messageContent.Length > _configuration.MaxMessageSize)
                {
                    _logger.LogWarning("Message from client {ClientId} exceeds maximum size ({Size} > {Max})",
                        _clientInfo.ClientId, messageContent.Length, _configuration.MaxMessageSize);
                    await SendResponseAsync("ERROR: Message too large");
                    return;
                }

                // Create TCP message object
                var tcpMessage = TcpMessage.Create(_clientInfo.ClientId, messageContent, _clientInfo.IpAddress, _clientInfo.Port);

                // Log raw message if enabled
                if (_configuration.LogRawMessages)
                {
                    _logger.LogDebug("Raw message from {ClientId}: {Message}", _clientInfo.ClientId, messageContent);
                }

                // Process message based on type
                await ProcessMessageByType(tcpMessage, cancellationToken);

                // Notify that message was received
                await NotifyMessageReceived(tcpMessage);

                _clientInfo.MessagesProcessed++;
                await SendResponseAsync("OK"); // Simple acknowledgment
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from client {ClientId}", _clientInfo.ClientId);
                await SendResponseAsync("ERROR: Processing failed");
                await NotifyErrorAsync(ex);
            }
        }

        private async Task ProcessMessageByType(TcpMessage message, CancellationToken cancellationToken)
        {
            switch (message.MessageType.ToUpperInvariant())
            {
                case "ALARM":
                    await ProcessAlarmMessage(message, cancellationToken);
                    break;
                case "HEARTBEAT":
                    await ProcessHeartbeatMessage(message, cancellationToken);
                    break;
                case "INVALID":
                    _logger.LogWarning("Invalid message format from client {ClientId}", _clientInfo.ClientId);
                    await SendResponseAsync("ERROR: Invalid message format");
                    break;
                default:
                    _logger.LogWarning("Unknown message type '{MessageType}' from client {ClientId}",
                        message.MessageType, _clientInfo.ClientId);
                    await SendResponseAsync("ERROR: Unknown message type");
                    break;
            }
        }

        private async Task ProcessAlarmMessage(TcpMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing alarm message from client {ClientId}", _clientInfo.ClientId);

            try
            {
                // Use the TcpMessageProcessorService to handle alarm processing
                bool success = await _messageProcessor.ProcessTcpMessageAsync(
                    _clientInfo.ClientId,
                    message.Content,
                    cancellationToken);

                if (success)
                {
                    _logger.LogInformation("Alarm processed successfully from client {ClientId}", _clientInfo.ClientId);
                    await SendResponseAsync("ALARM_OK");
                }
                else
                {
                    _logger.LogWarning("Failed to process alarm from client {ClientId}", _clientInfo.ClientId);
                    await SendResponseAsync("ALARM_ERROR");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alarm from client {ClientId}", _clientInfo.ClientId);
                await SendResponseAsync("ALARM_ERROR");
            }
        }

        private async Task ProcessHeartbeatMessage(TcpMessage message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Received heartbeat from client {ClientId}", _clientInfo.ClientId);

            // Simple heartbeat response
            await SendResponseAsync("HEARTBEAT_OK");
        }

        private async Task SendResponseAsync(string response)
        {
            try
            {
                if (!_clientInfo.IsConnected)
                    return;

                byte[] responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                await _clientInfo.Stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                await _clientInfo.Stream.FlushAsync();

                _logger.LogDebug("Sent response to client {ClientId}: {Response}", _clientInfo.ClientId, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending response to client {ClientId}", _clientInfo.ClientId);
            }
        }

        public async Task DisconnectAsync(string reason)
        {
            if (_clientInfo.Status == ConnectionStatus.Disconnected)
                return;

            _logger.LogInformation("Disconnecting client {ClientId}. Reason: {Reason}", _clientInfo.ClientId, reason);

            try
            {
                _clientInfo.Status = ConnectionStatus.Disconnected;

                // Log disconnection
                await _connectionLogService.LogClientDisconnectedAsync(
                    Guid.Parse(_clientInfo.ClientId), // Assuming ClientId can be parsed as Guid
                    reason);

                // Notify disconnection
                await NotifyClientDisconnected();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during client {ClientId} disconnection", _clientInfo.ClientId);
            }
            finally
            {
                _clientInfo.Dispose();
            }
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                if (!_clientInfo.IsConnected)
                {
                    _logger.LogWarning("Cannot send message to disconnected client {ClientId}", _clientInfo.ClientId);
                    return;
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(message + "\n");
                await _clientInfo.Stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                await _clientInfo.Stream.FlushAsync();

                _logger.LogDebug("Sent message to client {ClientId}: {Message}", _clientInfo.ClientId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to client {ClientId}", _clientInfo.ClientId);
                await NotifyErrorAsync(ex);
            }
        }

        // Event notification methods
        private async Task NotifyClientDisconnected()
        {
            try
            {
                if (ClientDisconnected != null)
                    await ClientDisconnected.Invoke(_clientInfo.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying client disconnection for {ClientId}", _clientInfo.ClientId);
            }
        }

        private async Task NotifyMessageReceived(TcpMessage message)
        {
            try
            {
                if (MessageReceived != null)
                    await MessageReceived.Invoke(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying message received from {ClientId}", _clientInfo.ClientId);
            }
        }

        private async Task NotifyErrorAsync(Exception exception)
        {
            try
            {
                if (ErrorOccurred != null)
                    await ErrorOccurred.Invoke(_clientInfo.ClientId, exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying error for client {ClientId}", _clientInfo.ClientId);
            }
        }

        // Check if client connection is still alive
        public bool IsClientAlive()
        {
            try
            {
                if (!_clientInfo.IsConnected)
                    return false;

                // Simple check - if we haven't heard from client in timeout period, consider it dead
                return _clientInfo.TimeSinceLastActivity.TotalSeconds < _configuration.ConnectionTimeoutSeconds;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                _clientInfo?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing TcpClientHandler for {ClientId}", _clientInfo?.ClientId ?? "Unknown");
            }
        }
    }
}