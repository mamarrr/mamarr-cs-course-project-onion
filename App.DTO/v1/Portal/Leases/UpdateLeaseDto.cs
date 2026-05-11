namespace App.DTO.v1.Portal.Leases;

public class UpdateLeaseDto
{
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}
