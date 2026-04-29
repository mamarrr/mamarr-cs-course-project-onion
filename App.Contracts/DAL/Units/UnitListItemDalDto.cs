namespace App.Contracts.DAL.Units;

public sealed class UnitListItemDalDto
{
    public Guid Id { get; init; }
    public Guid PropertyId { get; init; }
    public string UnitNr { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
}
