namespace App.DTO.v1.Portal.Units;

public class UnitListItemDto
{
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public string Path { get; set; } = string.Empty;
}
