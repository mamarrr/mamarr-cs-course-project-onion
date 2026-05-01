namespace App.BLL.Contracts.Units.Models;

public class UnitListItemModel
{
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
}
