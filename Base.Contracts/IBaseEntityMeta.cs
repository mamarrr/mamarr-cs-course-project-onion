namespace Base.Contracts;

public interface IBaseEntityWithMeta : IBaseEntity<Guid>, IHasCreatedMeta<Guid>;

public interface IBaseEntityWithMeta<TKey> : IBaseEntity<TKey>, IHasCreatedUpdatedMeta<TKey> where TKey : IEquatable<TKey>
{
    
}