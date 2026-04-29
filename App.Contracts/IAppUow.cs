using Base.DAL.Contracts;

namespace App.Contracts;

public interface IAppUOW : IBaseUOW
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
