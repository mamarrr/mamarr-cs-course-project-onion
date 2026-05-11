namespace App.DTO.v1.Portal.Units;

public class UnitProfileDto
{
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}
