namespace App.BLL.DTO.ManagementCompanies.Models;

public class OwnershipTransferCandidate
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
    public bool IsEffective { get; set; }
}
