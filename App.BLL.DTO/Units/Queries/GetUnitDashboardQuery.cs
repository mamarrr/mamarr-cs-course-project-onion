namespace App.BLL.DTO.Units.Queries;

public class GetUnitDashboardQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string UnitSlug { get; init; } = default!;
}
