namespace App.DAL.DTO.Units;

public class UnitUpdateDalDto
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}
