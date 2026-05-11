namespace App.DTO.v1.Portal.Leases;

public class LeaseDto
{
    public Guid LeaseId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}
