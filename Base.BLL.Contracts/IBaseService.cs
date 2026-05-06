using Base.Contracts;
using Base.DAL.Contracts;
using FluentResults;

namespace Base.BLL.Contracts;

public interface IBaseService<TEntity> : IBaseService<Guid, TEntity>
    where TEntity : IBaseEntity<Guid>

{
}

public interface IBaseService<TKey, TEntity>
    where TKey : IEquatable<TKey>
    where TEntity : IBaseEntity<TKey>
{
    Task<Result<IEnumerable<TEntity>>> AllAsync(TKey parentId = default!, CancellationToken cancellationToken = default);
    Task<Result<TEntity>> FindAsync(TKey id, TKey parentId = default!, CancellationToken cancellationToken = default);

    Task<Result<TEntity>> UpdateAsync(TEntity entity, TKey parentId = default!, CancellationToken cancellationToken = default);

    Task<Result> RemoveAsync(TKey id, TKey parentId = default!, CancellationToken cancellationToken = default);
}
