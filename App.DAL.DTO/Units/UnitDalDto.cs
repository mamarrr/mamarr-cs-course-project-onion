using Base.Contracts;

namespace App.DAL.DTO.Units;

public class UnitDalDto : IBaseEntity
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; init; }
    public string UnitNr { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public string? Notes { get; init; }
    
    public DateTime CreatedAt { get; init; }
}
