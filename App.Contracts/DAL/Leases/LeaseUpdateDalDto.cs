namespace App.Contracts.DAL.Leases;

public sealed class LeaseUpdateDalDto
{
    public Guid LeaseId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}
