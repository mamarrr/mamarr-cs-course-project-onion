using Base.Contracts;

namespace Base.DAL.Contracts;

public interface IBaseRepository<TDalEntity> : IBaseRepository<Guid, TDalEntity>
    where TDalEntity : IBaseEntity<Guid>
{
}

public interface IBaseRepository<in TKey, TDALEntity>
    where TKey : IEquatable<TKey>
    where TDALEntity : IBaseEntity<TKey>
{
    Task<IEnumerable<TDALEntity>> AllAsync(TKey parentId);

    Task<TDALEntity?> FindAsync(TKey id, TKey parentId);

    void Add(TDALEntity entity);

    TDALEntity Update(TDALEntity entity);

    void Remove(TDALEntity entity);

    Task Remove(TKey id);
}
