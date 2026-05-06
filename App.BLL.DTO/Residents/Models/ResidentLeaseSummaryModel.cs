namespace App.BLL.DTO.Residents.Models;

public class ResidentLeaseSummaryModel
{
    public Guid LeaseId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public string PropertyName { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public string UnitSlug { get; init; } = default!;
    public Guid LeaseRoleId { get; init; }
    public string LeaseRoleCode { get; init; } = default!;
    public string LeaseRoleLabel { get; init; } = default!;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    
    public string? Notes { get; init; }
}
