using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;

namespace AlarmMonitoringSystem.Domain.Interfaces.Repositories
{
    public interface IClientRepository : IBaseRepository<Client>
    {
        Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Client>> GetClientsWithAlarmsAsync(CancellationToken cancellationToken = default);
        Task<bool> ClientIdExistsAsync(string clientId, CancellationToken cancellationToken = default);
        Task<Client?> GetByIpAddressAsync(string ipAddress, int port, CancellationToken cancellationToken = default);
        Task UpdateStatusAsync(Guid clientId, ConnectionStatus status, CancellationToken cancellationToken = default);
        Task UpdateLastConnectedAsync(Guid clientId, DateTime connectedAt, CancellationToken cancellationToken = default);
        Task UpdateLastDisconnectedAsync(Guid clientId, DateTime disconnectedAt, CancellationToken cancellationToken = default);
    }
}
