namespace App.DTO.v1.Portal.Leases;

public class ResidentLeaseListItemDto
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public string UnitSlug { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = string.Empty;
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}
