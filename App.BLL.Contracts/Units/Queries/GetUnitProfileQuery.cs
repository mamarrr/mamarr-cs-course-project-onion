namespace App.BLL.Contracts.Units.Queries;

public sealed class GetUnitProfileQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string UnitSlug { get; init; } = default!;
}
