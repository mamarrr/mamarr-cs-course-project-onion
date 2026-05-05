
using Base.Contracts;

namespace Base.DAL.Contracts;

public interface IBaseRepository<TEntity> : IBaseRepository<Guid, TEntity> 
    where TEntity : IBaseEntity<Guid>
{
}

public interface IBaseRepository<TKey, TEntity>
    where TKey : IEquatable<TKey>
    where TEntity : IBaseEntity<TKey>
{
    Task<IEnumerable<TEntity>> AllAsync(TKey parentId = default!, CancellationToken cancellationToken = default);
    Task<TEntity?> FindAsync(TKey id, TKey parentId = default!, CancellationToken cancellationToken = default);

    TKey Add(TEntity entity);

    Task<TEntity> UpdateAsync(TEntity entity, TKey parentId = default!, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);
    Task RemoveAsync(TKey id, TKey parentId = default!, CancellationToken cancellationToken = default);
}
