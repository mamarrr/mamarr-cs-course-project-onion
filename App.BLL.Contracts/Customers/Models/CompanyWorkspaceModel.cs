namespace App.BLL.Contracts.Customers.Models;

public sealed class CompanyWorkspaceModel
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
}
