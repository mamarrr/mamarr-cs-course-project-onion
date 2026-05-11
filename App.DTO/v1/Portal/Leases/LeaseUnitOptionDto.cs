namespace App.DTO.v1.Portal.Leases;

public class LeaseUnitOptionDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
}
