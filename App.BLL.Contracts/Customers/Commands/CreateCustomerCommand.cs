namespace App.BLL.Contracts.Customers.Commands;

public class CreateCustomerCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Phone { get; init; }
}
