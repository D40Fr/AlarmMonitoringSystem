// AlarmMonitoringSystem.Infrastructure/Data/UnitOfWork/UnitOfWork.cs
using AlarmMonitoringSystem.Domain.Interfaces.Repositories;
using AlarmMonitoringSystem.Infrastructure.Data.Context;
using AlarmMonitoringSystem.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AlarmMonitoringSystem.Infrastructure.Data.UnitOfWork
{
    public class UnitOfWorkk : IUnitOfWork, IDisposable
    {
        private readonly AlarmMonitoringDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repository instances
        private IClientRepository? _clients;
        private IAlarmRepository? _alarms;
        private IConnectionLogRepository? _connectionLogs;

        public UnitOfWorkk(AlarmMonitoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IClientRepository Clients
        {
            get
            {
                _clients ??= new ClientRepository(_context);
                return _clients;
            }
        }

        public IAlarmRepository Alarms
        {
            get
            {
                _alarms ??= new AlarmRepository(_context);
                return _alarms;
            }
        }

        public IConnectionLogRepository ConnectionLogs
        {
            get
            {
                _connectionLogs ??= new ConnectionLogRepository(_context);
                return _connectionLogs;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new InvalidOperationException("A concurrency conflict occurred while saving changes.", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database update exceptions
                throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
            }
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction is in progress.");
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}