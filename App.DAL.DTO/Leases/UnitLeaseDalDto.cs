namespace App.DAL.DTO.Leases;

public class UnitLeaseDalDto
{
    public Guid LeaseId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public string ResidentFullName { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
    public Guid LeaseRoleId { get; init; }
    public string LeaseRoleCode { get; init; } = default!;
    public string LeaseRoleLabel { get; init; } = default!;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}
