namespace App.DTO.v1.Portal.Leases;

public class LeaseRoleOptionDto
{
    public Guid LeaseRoleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
