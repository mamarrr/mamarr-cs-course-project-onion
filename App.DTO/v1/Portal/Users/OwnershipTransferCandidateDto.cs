namespace App.DTO.v1.Portal.Users;

public class OwnershipTransferCandidateDto
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsEffective { get; set; }
}
