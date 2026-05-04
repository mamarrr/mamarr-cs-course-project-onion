namespace App.DAL.Contracts.DAL.Customers;

public class CustomerCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; } = true;
}
