namespace App.BLL.DTO.Leases.Models;

public class LeaseUnitOptionModel
{
    public Guid UnitId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
}
