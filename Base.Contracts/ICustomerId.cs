namespace Base.Contracts;

public interface ICustomerId : ICustomerId<Guid>
{ }

public interface ICustomerId<TKey> where TKey : IEquatable<TKey>
{
    public TKey CustomerId { get; set; }
}