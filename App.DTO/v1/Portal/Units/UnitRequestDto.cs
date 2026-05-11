namespace App.DTO.v1.Portal.Units;

public class UnitRequestDto
{
    public string UnitNr { get; set; } = default!;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public string? Notes { get; set; }
}
