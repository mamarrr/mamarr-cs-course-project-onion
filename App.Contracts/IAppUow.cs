using Base.DAL.Contracts;
using App.Contracts.DAL.Lookups;

namespace App.Contracts;

public interface IAppUOW : IBaseUOW
{
    ILookupRepository Lookups { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
