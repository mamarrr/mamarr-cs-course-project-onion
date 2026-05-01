namespace App.Contracts.DAL.Units;

public class UnitDashboardDalDto
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string PropertyName { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public string Slug { get; init; } = default!;
}
