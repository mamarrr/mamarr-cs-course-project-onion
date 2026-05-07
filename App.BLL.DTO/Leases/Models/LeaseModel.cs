namespace App.BLL.DTO.Leases.Models;

public class LeaseModel
{
    public Guid LeaseId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid UnitId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}
