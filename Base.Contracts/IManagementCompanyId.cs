namespace Base.Contracts;

public interface IManagementCompanyId : IManagementCompanyId<Guid>
{ }

public interface IManagementCompanyId<TKey> where TKey : IEquatable<TKey>
{
    public TKey ManagementCompanyId { get; set; }
}