namespace App.Contracts.DAL.Customers;

public sealed class CustomerListItemDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
