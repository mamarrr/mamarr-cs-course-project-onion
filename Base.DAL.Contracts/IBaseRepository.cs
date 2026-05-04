
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
    Task<IEnumerable<TEntity>> AllAsync(TKey appUserId = default!);
    Task<TEntity?> FindAsync(TKey id, TKey appUserId = default!);

    void Add(TEntity entity);

    TEntity Update(TEntity entity);

    void Remove(TEntity entity);
    Task RemoveAsync(TKey id);
}