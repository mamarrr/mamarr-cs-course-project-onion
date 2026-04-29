namespace App.BLL.Contracts.Units.Models;

public sealed class UnitWorkspaceModel
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
    public Guid UnitId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
}
