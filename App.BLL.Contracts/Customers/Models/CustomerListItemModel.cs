namespace App.BLL.Contracts.Customers.Models;

public class CustomerListItemModel
{
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<CustomerPropertyLinkModel> Properties { get; init; } = Array.Empty<CustomerPropertyLinkModel>();
}
