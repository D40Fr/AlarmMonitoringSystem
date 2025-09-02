using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.ValueObjects;


namespace AlarmMonitoringSystem.Domain.Interfaces.Services
{
    public interface IClientService
    {
        // Client management
        Task<Client> RegisterClientAsync(ClientInfo clientInfo, CancellationToken cancellationToken = default);
        Task<Client?> GetClientAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<Client?> GetClientByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetAllClientsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetClientsByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default);

        // Connection status management
        Task UpdateClientStatusAsync(Guid clientId, ConnectionStatus status, CancellationToken cancellationToken = default);
        Task UpdateClientStatusAsync(string clientId, ConnectionStatus status, CancellationToken cancellationToken = default);
        Task<bool> IsClientConnectedAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<bool> IsClientActiveAsync(Guid clientId, CancellationToken cancellationToken = default);

        // Client operations
        Task<Client> UpdateClientAsync(Client client, CancellationToken cancellationToken = default);
        Task DeactivateClientAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task ActivateClientAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task<bool> ClientExistsAsync(string clientId, CancellationToken cancellationToken = default);

        // Statistics
        Task<int> GetConnectedClientCountAsync(CancellationToken cancellationToken = default);
        Task<int> GetTotalClientCountAsync(CancellationToken cancellationToken = default);
        Task<Dictionary<ConnectionStatus, int>> GetClientStatusCountsAsync(CancellationToken cancellationToken = default);
    }
}
