namespace App.BLL.Contracts.Units.Commands;

public sealed class CreateUnitCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public string? Notes { get; init; }
}
