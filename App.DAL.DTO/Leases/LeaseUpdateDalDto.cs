namespace App.DAL.DTO.Leases;

public class LeaseUpdateDalDto
{
    public Guid LeaseId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    
    public string? Notes { get; init; }
}
