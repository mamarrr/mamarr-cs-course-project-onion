namespace App.DAL.DTO.Leases;

public class LeaseUnitOptionDalDto
{
    public Guid UnitId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
    public bool IsActive { get; init; }
}
