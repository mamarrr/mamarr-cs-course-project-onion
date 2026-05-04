using Base.Contracts;
using Base.DAL.Contracts;

namespace Base.BLL.Contracts;

public interface IBaseService<TEntity> : IBaseService<Guid, TEntity>
    where TEntity : IBaseEntity<Guid>

{
}

public interface IBaseService<TKey, TEntity> : IBaseRepository<TKey, TEntity>
    where TKey : IEquatable<TKey>
    where TEntity : IBaseEntity<TKey>
{
}