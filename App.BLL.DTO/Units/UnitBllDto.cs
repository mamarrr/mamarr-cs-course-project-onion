using Base.Domain;

namespace App.BLL.DTO.Units;

public class UnitBllDto : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string UnitNr { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public string? Notes { get; set; }
}

