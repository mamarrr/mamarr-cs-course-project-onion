using App.Contracts;
using App.Contracts.DAL.Customers;
using App.Contracts.DAL.Lookups;
using App.DAL.EF.Repositories;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.DAL.EF;

public class AppUOW : BaseUOW<AppDbContext>, IAppUOW
{
    private IDbContextTransaction? _transaction;
    private ICustomerRepository? _customers;
    private ILookupRepository? _lookups;

    public AppUOW(AppDbContext dbContext) : base(dbContext)
    {
    }

    public ICustomerRepository Customers => _customers ??= new CustomerRepository(UowDbContext);

    public ILookupRepository Lookups => _lookups ??= new LookupRepository(UowDbContext);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _transaction = await UowDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction exists.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction exists.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
