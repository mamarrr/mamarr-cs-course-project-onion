namespace App.BLL.Contracts.Customers.Commands;

public sealed class UpdateCustomerProfileCommand
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
