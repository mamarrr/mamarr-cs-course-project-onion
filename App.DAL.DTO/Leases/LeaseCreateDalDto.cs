namespace App.DAL.DTO.Leases;

public class LeaseCreateDalDto
{
    public Guid UnitId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}
