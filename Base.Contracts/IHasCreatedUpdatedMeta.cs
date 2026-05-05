namespace Base.Contracts;

public interface IHasCreatedUpdatedMeta : IHasCreatedMeta<Guid>, IHasUpdateMeta<Guid>;

public interface IHasCreatedUpdatedMeta<TKey> : IHasCreatedMeta<TKey>, IHasUpdateMeta<TKey> where TKey : IEquatable<TKey>
{
}