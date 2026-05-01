namespace App.BLL.Contracts.Properties.Models;

public class PropertyWorkspaceModel
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid CustomerId { get; init; }
    public string CustomerSlug { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
    public Guid PropertyId { get; init; }
    public string PropertySlug { get; init; } = default!;
    public string PropertyName { get; init; } = default!;
}
