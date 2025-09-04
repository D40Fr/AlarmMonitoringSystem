// AlarmMonitoringSystem.Infrastructure/Data/Repositories/ClientRepository.cs
using AlarmMonitoringSystem.Domain.Entities;
using AlarmMonitoringSystem.Domain.Enums;
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace AlarmMonitoringSystem.Infrastructure.Data.Repositories
{
    public class ClientRepository : BaseRepository<Client>, IClientRepository
    {
        public ClientRepository(AlarmMonitoringDbContext context) : base(context)
        {
        }

        public async Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return await _dbSet
                .Include(c => c.Alarms)
                .Include(c => c.ConnectionLogs)
                .FirstOrDefaultAsync(c => c.ClientId == clientId.Trim(), cancellationToken);
        }

        public async Task<IEnumerable<Client>> GetByStatusAsync(ConnectionStatus status, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(c => c.Status == status)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Client>> GetClientsWithAlarmsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Alarms.Where(a => a.IsActive))
                .Where(c => c.Alarms.Any(a => a.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ClientIdExistsAsync(string clientId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return false;

            return await _dbSet
                .AnyAsync(c => c.ClientId == clientId.Trim(), cancellationToken);
        }

        public async Task<Client?> GetByIpAddressAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(c => c.IpAddress == ipAddress.Trim() && c.Port == port, cancellationToken);
        }

        public async Task UpdateStatusAsync(Guid clientId, ConnectionStatus status, CancellationToken cancellationToken = default)
        {
            var client = await _dbSet.FindAsync(new object[] { clientId }, cancellationToken);
            if (client != null)
            {
                client.Status = status;
                client.UpdatedAt = DateTime.UtcNow;

                // Update connection timestamps
                var now = DateTime.UtcNow;
                switch (status)
                {
                    case ConnectionStatus.Connected:
                        client.LastConnectedAt = now;
                        break;
                }
            }
        }

        public async Task UpdateLastConnectedAsync(Guid clientId, DateTime connectedAt, CancellationToken cancellationToken = default)
        {
            var client = await _dbSet.FindAsync(new object[] { clientId }, cancellationToken);
            if (client != null)
            {
                client.LastConnectedAt = connectedAt;
                client.Status = ConnectionStatus.Connected;
                client.UpdatedAt = DateTime.UtcNow;
            }
        }

        public async Task UpdateLastDisconnectedAsync(Guid clientId, DateTime disconnectedAt, CancellationToken cancellationToken = default)
        {
            var client = await _dbSet.FindAsync(new object[] { clientId }, cancellationToken);
            if (client != null)
            {
                client.Status = ConnectionStatus.Disconnected;
                client.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Override GetByIdAsync to include related data
        public override async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Alarms.Where(a => a.IsActive))
                .Include(c => c.ConnectionLogs.OrderByDescending(cl => cl.LogTime).Take(10))
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        // Get clients with statistics
        public async Task<IEnumerable<Client>> GetClientsWithStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(c => c.Alarms.Where(a => a.IsActive && !a.IsAcknowledged))
                .Select(c => new Client
                {
                    Id = c.Id,
                    ClientId = c.ClientId,
                    Name = c.Name,
                    Description = c.Description,
                    IpAddress = c.IpAddress,
                    Port = c.Port,
                    Status = c.Status,
                    LastConnectedAt = c.LastConnectedAt,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Alarms = c.Alarms.Where(a => a.IsActive && !a.IsAcknowledged).ToList()
                })
                .ToListAsync(cancellationToken);
        }

        // Get connected clients count
        public async Task<int> GetConnectedClientsCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(c => c.Status == ConnectionStatus.Connected && c.IsActive, cancellationToken);
        }

        // Get clients by multiple statuses
        public async Task<IEnumerable<Client>> GetClientsByStatusesAsync(IEnumerable<ConnectionStatus> statuses, CancellationToken cancellationToken = default)
        {
            var statusList = statuses.ToList();
            return await _dbSet
                .Where(c => statusList.Contains(c.Status))
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        // Get clients that haven't connected recently
        public async Task<IEnumerable<Client>> GetInactiveClientsAsync(TimeSpan inactiveThreshold, CancellationToken cancellationToken = default)
        {
            var cutoffTime = DateTime.UtcNow - inactiveThreshold;

            return await _dbSet
                .Where(c => c.IsActive &&
                           (c.LastConnectedAt == null || c.LastConnectedAt < cutoffTime) &&
                           c.Status != ConnectionStatus.Connected)
                .OrderBy(c => c.LastConnectedAt ?? c.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}