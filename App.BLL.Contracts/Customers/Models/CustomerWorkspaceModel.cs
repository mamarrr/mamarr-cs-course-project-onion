namespace App.BLL.Contracts.Customers.Models;

public sealed class CustomerWorkspaceModel
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid CustomerId { get; init; }
    public string CustomerSlug { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
}
