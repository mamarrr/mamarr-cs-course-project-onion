namespace Base.Contracts;

public interface IHasUpdateMeta : IHasUpdateMeta<Guid>;

public interface IHasUpdateMeta<TKey> where TKey : IEquatable<TKey>
{
    public DateTime? UpdatedAt { get; set; }
    public TKey? UpdatedBy { get; set; }
}