using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Application.DTOs;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Domain.Interfaces.Services;
using AlarmMonitoringSystem.Domain.ValueObjects;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AlarmMonitoringSystem.Application.Services
{
    public class ClientService : IClientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ClientService> _logger;

        public ClientService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ClientService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Client> RegisterClientAsync(ClientInfo clientInfo, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Registering client: {ClientId}", clientInfo.ClientId);

            // Check if client already exists
            var existingClient = await _unitOfWork.Clients.GetByClientIdAsync(clientInfo.ClientId, cancellationToken);
            if (existingClient != null)
            {
                _logger.LogWarning("Client {ClientId} already exists", clientInfo.ClientId);
                throw new InvalidOperationException($"Client with ID '{clientInfo.ClientId}' already exists.");
            }

            // Create new client
            var client = new Client
            {
                ClientId = clientInfo.ClientId,
                Name = clientInfo.Name,
                Description = clientInfo.Description,
                IpAddress = clientInfo.IpAddress,
                Port = clientInfo.Port,
                Status = clientInfo.Status,
                IsActive = clientInfo.IsActive,
                LastConnectedAt = clientInfo.LastConnectedAt,
                LastDisconnectedAt = clientInfo.LastDisconnectedAt
            };

            await _unitOfWork.Clients.AddAsync(client, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Client {ClientId} registered successfully", clientInfo.ClientId);
            return client;
        }

        public async Task<Client?> GetClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken);
        }

        public async Task<Client?> GetClientByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.GetByClientIdAsync(clientId, cancellationToken);
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.GetAllAsync(cancellationToken);
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.GetActiveClientsAsync(cancellationToken);
        }

        public async Task<IEnumerable<Client>> GetClientsByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.GetByStatusAsync(status, cancellationToken);
        }

        public async Task UpdateClientStatusAsync(Guid clientId, ConnectionStatus status, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating client {ClientId} status to {Status}", clientId, status);

            await _unitOfWork.Clients.UpdateStatusAsync(clientId, status, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Client {ClientId} status updated to {Status}", clientId, status);
        }

        public async Task UpdateClientStatusAsync(string clientId, ConnectionStatus status, CancellationToken cancellationToken = default)
        {
            var client = await _unitOfWork.Clients.GetByClientIdAsync(clientId, cancellationToken);
            if (client == null)
            {
                _logger.LogWarning("Client {ClientId} not found for status update", clientId);
                throw new InvalidOperationException($"Client with ID '{clientId}' not found.");
            }

            await UpdateClientStatusAsync(client.Id, status, cancellationToken);
        }

        public async Task<bool> IsClientConnectedAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            var client = await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken);
            return client?.Status == ConnectionStatus.Connected;
        }

        public async Task<bool> IsClientActiveAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            var client = await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken);
            return client?.IsActive == true;
        }

        public async Task<Client> UpdateClientAsync(Client client, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating client: {ClientId}", client.ClientId);

            var updatedClient = await _unitOfWork.Clients.UpdateAsync(client, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Client {ClientId} updated successfully", client.ClientId);
            return updatedClient;
        }

        public async Task DeactivateClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            var client = await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken);
            if (client != null)
            {
                client.IsActive = false;
                await _unitOfWork.Clients.UpdateAsync(client, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Client {ClientId} deactivated", client.ClientId);
            }
        }

        public async Task ActivateClientAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            var client = await _unitOfWork.Clients.GetByIdAsync(clientId, cancellationToken);
            if (client != null)
            {
                client.IsActive = true;
                await _unitOfWork.Clients.UpdateAsync(client, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Client {ClientId} activated", client.ClientId);
            }
        }

        public async Task<bool> ClientExistsAsync(string clientId, CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.ClientIdExistsAsync(clientId, cancellationToken);
        }

        public async Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default)
        {
            var connectedClients = await _unitOfWork.Clients.GetByStatusAsync(ConnectionStatus.Connected, cancellationToken);
            return connectedClients.Count();
        }

        public async Task<int> GetTotalClientCountAsync(CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Clients.CountAsync(cancellationToken);
        }

        public async Task<Dictionary<ConnectionStatus, int>> GetClientStatusCountsAsync(CancellationToken cancellationToken = default)
        {
            var allClients = await _unitOfWork.Clients.GetAllAsync(cancellationToken);
            return allClients
                .GroupBy(c => c.Status)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
