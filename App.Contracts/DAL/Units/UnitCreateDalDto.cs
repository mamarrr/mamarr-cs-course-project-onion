namespace App.Contracts.DAL.Units;

public class UnitCreateDalDto
{
    public Guid PropertyId { get; init; }
    public string UnitNr { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public string? Notes { get; init; }
}
