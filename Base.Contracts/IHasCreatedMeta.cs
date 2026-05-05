namespace Base.Contracts;

public interface IHasCreatedMeta : IHasCreatedMeta<Guid>;

public interface IHasCreatedMeta<TKey> where TKey : IEquatable<TKey>
{
    public DateTime CreatedAt { get; set; }
    public TKey CreatedBy { get; set; }
}