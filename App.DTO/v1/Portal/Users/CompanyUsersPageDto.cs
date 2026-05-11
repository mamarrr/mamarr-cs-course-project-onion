namespace App.DTO.v1.Portal.Users;

public class CompanyUsersPageDto
{
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid ActorMembershipId { get; set; }
    public Guid ActorRoleId { get; set; }
    public string ActorRoleCode { get; set; } = string.Empty;
    public string ActorRoleLabel { get; set; } = string.Empty;
    public bool CurrentActorIsOwner { get; set; }
    public bool CurrentActorIsAdmin { get; set; }
    public IReadOnlyList<CompanyUserListItemDto> Members { get; set; } = [];
    public IReadOnlyList<PendingAccessRequestDto> PendingRequests { get; set; } = [];
    public IReadOnlyList<CompanyUserRoleOptionDto> Roles { get; set; } = [];
}
