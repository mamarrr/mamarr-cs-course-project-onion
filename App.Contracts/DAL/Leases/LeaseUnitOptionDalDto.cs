namespace App.Contracts.DAL.Leases;

public sealed class LeaseUnitOptionDalDto
{
    public Guid UnitId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
    public bool IsActive { get; init; }
}
